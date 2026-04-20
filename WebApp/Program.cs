using MudBlazor;
using MudBlazor.Services;
using NORCE.Drilling.WellBore.WebApp;
using NORCE.Drilling.WellBore.WebPages;

var builder = WebApplication.CreateBuilder(args);

WebPagesHostConfiguration webPagesConfiguration = new()
{
    WellBoreHostURL = builder.Configuration["WellBoreHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty,
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    WellHostURL = builder.Configuration["WellHostURL"] ?? string.Empty,
    TrajectoryHostURL = builder.Configuration["TrajectoryHostURL"] ?? string.Empty,
    CartographicProjectionHostURL = builder.Configuration["CartographicProjectionHostURL"] ?? string.Empty,
    GeodeticDatumHostURL = builder.Configuration["GeodeticDatumHostURL"] ?? string.Empty,
};

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
builder.Services.AddSingleton<IWellBoreWebPagesConfiguration>(webPagesConfiguration);
builder.Services.AddSingleton<IWellBoreAPIUtils, WellBoreAPIUtils>();
builder.Services.AddExternalWebPages(webPagesConfiguration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UsePathBase("/WellBore/webapp");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
