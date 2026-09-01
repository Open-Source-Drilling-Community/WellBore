using OSDC.Drilling.WellBore.Model;
using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Service.Managers;

internal enum WellBoreMutationFailureKind
{
    None,
    InvalidRequest,
    NotFound,
    Conflict,
    StorageFailure
}

internal sealed record WellBoreMutationResult(
    WellBoreMutationFailureKind FailureKind,
    WellBoreMutationErrorEnvelope? Error = null,
    Model.WellBore? Resource = null)
{
    public bool Succeeded => FailureKind == WellBoreMutationFailureKind.None;

    public static WellBoreMutationResult Success(Model.WellBore? resource = null) => new(WellBoreMutationFailureKind.None, Resource: resource);

    public static WellBoreMutationResult Invalid(string property, string code, string message) =>
        Failure(WellBoreMutationFailureKind.InvalidRequest, "invalid_request", "The mutation request is invalid.", property, code, message);

    public static WellBoreMutationResult NotFound(string message) =>
        new(WellBoreMutationFailureKind.NotFound, new WellBoreMutationErrorEnvelope
        {
            Error = "not_found",
            Message = message
        });

    public static WellBoreMutationResult AlreadyExists(string message) =>
        new(WellBoreMutationFailureKind.Conflict, new WellBoreMutationErrorEnvelope
        {
            Error = "already_exists",
            Message = message
        });

    public static WellBoreMutationResult ConcurrencyConflict(string property, string message) =>
        Failure(WellBoreMutationFailureKind.Conflict, "concurrency_conflict", "The resource was modified by another caller.",
            property, "concurrency_conflict", message);

    public static WellBoreMutationResult ReferenceConflict(WellBoreMutationError error) =>
        new(WellBoreMutationFailureKind.Conflict, new WellBoreMutationErrorEnvelope
        {
            Error = "reference_conflict",
            Message = "The mutation would break a WellBore-owned catalog reference.",
            Errors = [error]
        });

    public static WellBoreMutationResult InvalidReferences(List<WellBoreMutationError> errors) =>
        new(WellBoreMutationFailureKind.InvalidRequest, new WellBoreMutationErrorEnvelope
        {
            Error = "invalid_reference",
            Message = "One or more WellBore-owned catalog references are invalid.",
            Errors = errors
        });

    public static WellBoreMutationResult InvalidWellBore(List<WellBoreMutationError> errors) =>
        new(WellBoreMutationFailureKind.InvalidRequest, new WellBoreMutationErrorEnvelope
        {
            Error = "invalid_well",
            Message = "The WellBore document violates one or more invariants.",
            Errors = errors
        });

    public static WellBoreMutationResult StorageFailure() =>
        new(WellBoreMutationFailureKind.StorageFailure, new WellBoreMutationErrorEnvelope
        {
            Error = "storage_failure",
            Message = "The mutation could not be committed. No partial change was retained."
        });

    private static WellBoreMutationResult Failure(WellBoreMutationFailureKind kind, string error, string summary,
        string property, string code, string message) =>
        new(kind, new WellBoreMutationErrorEnvelope
        {
            Error = error,
            Message = summary,
            Errors = [new WellBoreMutationError { Property = property, Code = code, Message = message }]
        });
}


