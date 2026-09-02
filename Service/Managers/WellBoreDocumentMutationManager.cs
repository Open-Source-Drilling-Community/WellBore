using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.WellBore.Model;
using System;
using System.Linq;
using System.Text.Json;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.Service.Managers;

/// <summary>Mutates WellBore documents atomically with validation and optimistic concurrency.</summary>
internal static class WellBoreDocumentMutationManager
{
    private static readonly DateTimeOffset LegacyRevision = DateTimeOffset.UnixEpoch;

    public static WellBoreMutationResult Create(SqlConnectionManager manager, ILogger logger, WellBoreModel? wellBore)
    {
        if (wellBore?.MetaInfo == null || wellBore.MetaInfo.ID == Guid.Empty)
            return WellBoreMutationResult.Invalid("MetaInfo.ID", "invalid_id", "A caller-generated non-empty WellBore UUID is required.");

        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null) return WellBoreMutationResult.StorageFailure();
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            if (Read(connection, transaction, wellBore.MetaInfo.ID) != null)
            {
                transaction.Rollback();
                return WellBoreMutationResult.AlreadyExists($"A WellBore with UUID '{wellBore.MetaInfo.ID}' already exists.");
            }
            WellBoreSidetrackClassification.Synchronize(connection, transaction, wellBore, featureIsCanonical: false);
            var errors = WellBoreReferenceIntegrityValidator.ValidateWellBore(connection, transaction, wellBore);
            if (errors.Count != 0)
            {
                transaction.Rollback();
                return WellBoreMutationResult.InvalidWellBore(errors);
            }
            DateTimeOffset now = DateTimeOffset.UtcNow;
            wellBore.CreationDate = now;
            wellBore.LastModificationDate = now;
            using SqliteCommand command = CreateWriteCommand(connection, transaction, wellBore, true);
            if (command.ExecuteNonQuery() != 1)
            {
                transaction.Rollback();
                return WellBoreMutationResult.StorageFailure();
            }
            transaction.Commit();
            return WellBoreMutationResult.Success(wellBore);
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            TryRollback(transaction);
            logger.LogError(ex, "Unable to create WellBore {WellBoreId}", wellBore.MetaInfo.ID);
            return WellBoreMutationResult.StorageFailure();
        }
    }

    public static WellBoreMutationResult Update(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreModel? wellBore)
    {
        if (id == Guid.Empty || wellBore?.MetaInfo == null || wellBore.MetaInfo.ID != id)
            return WellBoreMutationResult.Invalid("MetaInfo.ID", "id_mismatch", "The route UUID must be non-empty and equal MetaInfo.ID.");
        return Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            wellBore.CreationDate = stored.CreationDate;
            return wellBore;
        });
    }

    public static WellBoreMutationResult UpdateDetails(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreDetailsUpdate? details) =>
        Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            if (details == null) return null;
            stored.Name = details.Name;
            stored.Description = details.Description;
            return stored;
        }, details == null ? WellBoreMutationResult.Invalid("details", "required", "The WellBore details are required.") : null);

    public static WellBoreMutationResult UpdateTopology(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreTopologyUpdate? topology) =>
        Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            if (topology == null) return null;
            stored.WellID = topology.WellID;
            stored.RigID = topology.RigID;
            stored.IsSidetrack = topology.IsSidetrack;
            stored.ParentWellBoreID = topology.ParentWellBoreID;
            stored.TieInPointAlongHoleDepth = topology.TieInPointAlongHoleDepth;
            stored.SidetrackType = topology.SidetrackType;
            return stored;
        }, topology == null ? WellBoreMutationResult.Invalid("topology", "required", "The WellBore topology is required.") : null,
        legacyTypeIsExplicit: true);

    public static WellBoreMutationResult AddIdentityAssignment(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreIdentityAssignment? assignment) =>
        Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            if (assignment == null) return null;
            if (AssignmentIdExists(stored, assignment.ID)) return null;
            (stored.WellBoreIdentityAssignments ??= []).Add(assignment);
            return stored;
        }, assignment == null
            ? WellBoreMutationResult.Invalid("assignment", "required", "An identity assignment is required.")
            : assignment.ID == Guid.Empty
                ? WellBoreMutationResult.Invalid("assignment.ID", "invalid_id", "A caller-generated non-empty assignment UUID is required.")
                : null,
        stored => AssignmentIdExists(stored, assignment!.ID)
            ? WellBoreMutationResult.AlreadyExists($"Assignment UUID '{assignment.ID}' already exists on this WellBore.") : null);

    public static WellBoreMutationResult UpdateIdentityAssignment(SqlConnectionManager manager, ILogger logger, Guid id,
        Guid assignmentId, DateTimeOffset expectedModifiedUtc, WellBoreIdentityAssignment? assignment) =>
        MutateAssignment(manager, logger, id, assignmentId, expectedModifiedUtc, assignment,
            well => well.WellBoreIdentityAssignments ??= [], "identity");

    public static WellBoreMutationResult DeleteIdentityAssignment(SqlConnectionManager manager, ILogger logger, Guid id,
        Guid assignmentId, DateTimeOffset expectedModifiedUtc) =>
        DeleteAssignment(manager, logger, id, assignmentId, expectedModifiedUtc,
            well => well.WellBoreIdentityAssignments ??= [], "identity");

    public static WellBoreMutationResult AddFeatureAssignment(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, WellBoreFeatureAssignment? assignment) =>
        Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            if (assignment == null) return null;
            if (AssignmentIdExists(stored, assignment.ID)) return null;
            (stored.WellBoreFeatureAssignments ??= []).Add(assignment);
            return stored;
        }, assignment == null
            ? WellBoreMutationResult.Invalid("assignment", "required", "A feature assignment is required.")
            : assignment.ID == Guid.Empty
                ? WellBoreMutationResult.Invalid("assignment.ID", "invalid_id", "A caller-generated non-empty assignment UUID is required.")
                : null,
        stored => AssignmentIdExists(stored, assignment!.ID)
            ? WellBoreMutationResult.AlreadyExists($"Assignment UUID '{assignment.ID}' already exists on this WellBore.") : null,
        isFeatureMutation: true);

    public static WellBoreMutationResult UpdateFeatureAssignment(SqlConnectionManager manager, ILogger logger, Guid id,
        Guid assignmentId, DateTimeOffset expectedModifiedUtc, WellBoreFeatureAssignment? assignment) =>
        MutateAssignment(manager, logger, id, assignmentId, expectedModifiedUtc, assignment,
            well => well.WellBoreFeatureAssignments ??= [], "feature", isFeatureMutation: true);

    public static WellBoreMutationResult DeleteFeatureAssignment(SqlConnectionManager manager, ILogger logger, Guid id,
        Guid assignmentId, DateTimeOffset expectedModifiedUtc) =>
        DeleteAssignment(manager, logger, id, assignmentId, expectedModifiedUtc,
            well => well.WellBoreFeatureAssignments ??= [], "feature", isFeatureMutation: true);

    public static WellBoreMutationResult Delete(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc)
    {
        if (id == Guid.Empty) return WellBoreMutationResult.Invalid("id", "invalid_id", "A non-empty WellBore UUID is required.");
        if (expectedModifiedUtc == default) return MissingRevision();
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null) return WellBoreMutationResult.StorageFailure();
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            WellBoreModel? stored = Read(connection, transaction, id);
            if (stored == null) { transaction.Rollback(); return WellBoreMutationResult.NotFound("The WellBore does not exist."); }
            WellBoreMutationResult? conflict = CheckRevision(stored, expectedModifiedUtc);
            if (conflict != null) { transaction.Rollback(); return conflict; }
            using (SqliteCommand children = connection.CreateCommand())
            {
                children.Transaction = transaction;
                children.CommandText = "SELECT COUNT(*) FROM WellBoreTable WHERE ParentWellBoreID=$id";
                children.Parameters.AddWithValue("$id", id.ToString());
                if (Convert.ToInt64(children.ExecuteScalar()) != 0)
                {
                    transaction.Rollback();
                    return WellBoreMutationResult.ReferenceConflict(new WellBoreMutationError
                    {
                        Property = "ParentWellBoreID", Code = "parent_in_use",
                        Message = "The WellBore is the parent of one or more sidetracks and cannot be deleted."
                    });
                }
            }
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM WellBoreTable WHERE ID=$id";
            command.Parameters.AddWithValue("$id", id.ToString());
            if (command.ExecuteNonQuery() != 1) { transaction.Rollback(); return WellBoreMutationResult.StorageFailure(); }
            transaction.Commit();
            return WellBoreMutationResult.Success();
        }
        catch (SqliteException ex)
        {
            TryRollback(transaction);
            logger.LogError(ex, "Unable to delete WellBore {WellBoreId}", id);
            return WellBoreMutationResult.StorageFailure();
        }
    }

    public static DateTimeOffset RevisionOf(WellBoreModel value) =>
        value.LastModificationDate ?? value.CreationDate ?? LegacyRevision;

    public static void EnsureRevision(WellBoreModel? value)
    {
        if (value == null) return;
        DateTimeOffset revision = RevisionOf(value);
        value.CreationDate ??= revision;
        value.LastModificationDate ??= revision;
    }

    private static WellBoreMutationResult MutateAssignment<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        Guid assignmentId, DateTimeOffset expectedModifiedUtc, T? assignment,
        Func<WellBoreModel, System.Collections.Generic.List<T>> select, string kind,
        bool isFeatureMutation = false) where T : class
    {
        if (assignmentId == Guid.Empty || assignment == null || AssignmentId(assignment) != assignmentId)
            return WellBoreMutationResult.Invalid("assignment.ID", "id_mismatch", "The route assignment UUID must be non-empty and equal assignment.ID.");
        return Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            var values = select(stored);
            int index = values.FindIndex(value => AssignmentId(value) == assignmentId);
            if (index < 0) return null;
            values[index] = assignment;
            return stored;
        }, null, stored => select(stored).Any(value => AssignmentId(value) == assignmentId)
            ? null : WellBoreMutationResult.NotFound($"The WellBore {kind} assignment does not exist."), isFeatureMutation);
    }

    private static WellBoreMutationResult DeleteAssignment<T>(SqlConnectionManager manager, ILogger logger, Guid id,
        Guid assignmentId, DateTimeOffset expectedModifiedUtc, Func<WellBoreModel, System.Collections.Generic.List<T>> select,
        string kind, bool isFeatureMutation = false) where T : class
    {
        if (assignmentId == Guid.Empty)
            return WellBoreMutationResult.Invalid("assignmentId", "invalid_id", "A non-empty assignment UUID is required.");
        return Mutate(manager, logger, id, expectedModifiedUtc, stored =>
        {
            var values = select(stored);
            int index = values.FindIndex(value => AssignmentId(value) == assignmentId);
            if (index < 0) return null;
            values.RemoveAt(index);
            return stored;
        }, null, stored => select(stored).Any(value => AssignmentId(value) == assignmentId)
            ? null : WellBoreMutationResult.NotFound($"The WellBore {kind} assignment does not exist."), isFeatureMutation);
    }

    private static WellBoreMutationResult Mutate(SqlConnectionManager manager, ILogger logger, Guid id,
        DateTimeOffset expectedModifiedUtc, Func<WellBoreModel, WellBoreModel?> mutation,
        WellBoreMutationResult? precondition = null, Func<WellBoreModel, WellBoreMutationResult?>? storedPrecondition = null,
        bool isFeatureMutation = false, bool legacyTypeIsExplicit = false)
    {
        if (precondition != null) return precondition;
        if (id == Guid.Empty) return WellBoreMutationResult.Invalid("wellBoreId", "invalid_id", "A non-empty WellBore UUID is required.");
        if (expectedModifiedUtc == default) return MissingRevision();
        using SqliteConnection? connection = manager.GetConnection();
        if (connection == null) return WellBoreMutationResult.StorageFailure();
        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            WellBoreModel? stored = Read(connection, transaction, id);
            if (stored == null) { transaction.Rollback(); return WellBoreMutationResult.NotFound("The WellBore does not exist."); }
            WellBoreMutationResult? conflict = CheckRevision(stored, expectedModifiedUtc);
            if (conflict != null) { transaction.Rollback(); return conflict; }
            WellBoreMutationResult? storedError = storedPrecondition?.Invoke(stored);
            if (storedError != null) { transaction.Rollback(); return storedError; }
            bool hadClassification = isFeatureMutation &&
                                     WellBoreSidetrackClassification.HasAssignment(connection, transaction, stored);
            WellBoreModel? updated = mutation(stored);
            if (updated == null) { transaction.Rollback(); return WellBoreMutationResult.Invalid("mutation", "invalid", "The requested mutation is invalid."); }
            bool classificationIsCanonical = isFeatureMutation && (hadClassification ||
                WellBoreSidetrackClassification.HasAssignment(connection, transaction, updated));
            WellBoreSidetrackClassification.Synchronize(connection, transaction, updated, classificationIsCanonical, legacyTypeIsExplicit);
            var errors = WellBoreReferenceIntegrityValidator.ValidateWellBore(connection, transaction, updated);
            if (errors.Count != 0) { transaction.Rollback(); return WellBoreMutationResult.InvalidWellBore(errors); }
            updated.CreationDate = stored.CreationDate;
            updated.LastModificationDate = NextRevision(RevisionOf(stored));
            using SqliteCommand command = CreateWriteCommand(connection, transaction, updated, false);
            if (command.ExecuteNonQuery() != 1) { transaction.Rollback(); return WellBoreMutationResult.StorageFailure(); }
            transaction.Commit();
            return WellBoreMutationResult.Success(updated);
        }
        catch (Exception ex) when (ex is SqliteException or JsonException)
        {
            TryRollback(transaction);
            logger.LogError(ex, "Unable to mutate WellBore {WellBoreId}", id);
            return WellBoreMutationResult.StorageFailure();
        }
    }

    private static WellBoreMutationResult? CheckRevision(WellBoreModel stored, DateTimeOffset expected)
    {
        DateTimeOffset actual = RevisionOf(stored);
        return actual.UtcTicks == expected.UtcTicks ? null : WellBoreMutationResult.ConcurrencyConflict(
            "expectedModifiedUtc", $"Expected {expected:O}, but the stored WellBore was modified at {actual:O}.");
    }

    private static WellBoreMutationResult MissingRevision() => WellBoreMutationResult.Invalid(
        "expectedModifiedUtc", "required", "A non-default optimistic-concurrency timestamp is required.");

    private static bool AssignmentIdExists(WellBoreModel value, Guid id) =>
        (value.WellBoreIdentityAssignments ?? []).Any(item => item?.ID == id) ||
        (value.WellBoreFeatureAssignments ?? []).Any(item => item?.ID == id);

    private static Guid AssignmentId<T>(T value) => value switch
    {
        WellBoreIdentityAssignment identity => identity.ID,
        WellBoreFeatureAssignment feature => feature.ID,
        _ => Guid.Empty
    };

    private static DateTimeOffset NextRevision(DateTimeOffset stored)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return now.UtcTicks > stored.UtcTicks ? now : stored.AddTicks(1);
    }

    private static WellBoreModel? Read(SqliteConnection connection, SqliteTransaction transaction, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT WellBore FROM WellBoreTable WHERE ID=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        WellBoreModel? value = command.ExecuteScalar() is string json
            ? JsonSerializer.Deserialize<WellBoreModel>(json, JsonSettings.Options) : null;
        EnsureRevision(value);
        return value;
    }

    private static SqliteCommand CreateWriteCommand(SqliteConnection connection, SqliteTransaction transaction,
        WellBoreModel value, bool insert)
    {
        SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = insert
            ? "INSERT INTO WellBoreTable (ID,MetaInfo,WellID,RigID,IsSidetrack,ParentWellBoreID,WellBore) VALUES ($id,$meta,$well,$rig,$sidetrack,$parent,$document)"
            : "UPDATE WellBoreTable SET MetaInfo=$meta,WellID=$well,RigID=$rig,IsSidetrack=$sidetrack,ParentWellBoreID=$parent,WellBore=$document WHERE ID=$id";
        command.Parameters.AddWithValue("$id", value.MetaInfo!.ID.ToString());
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("$well", value.WellID?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$rig", value.RigID?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$sidetrack", value.IsSidetrack ? 1 : 0);
        command.Parameters.AddWithValue("$parent", value.ParentWellBoreID?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$document", JsonSerializer.Serialize(value, JsonSettings.Options));
        return command;
    }

    private static void TryRollback(SqliteTransaction transaction)
    {
        try { transaction.Rollback(); } catch (InvalidOperationException) { }
    }
}
