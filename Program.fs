module SigmaChatServer.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open SigmaChatServer.Routing
open System.Data
open Microsoft.Extensions.Configuration
open Npgsql
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open Azure.Storage.Blobs
open Hub
open Microsoft.IdentityModel.Claims
open Azure.Storage.Blobs.Models
open Dapper.Extensions
open Newtonsoft.Json
open Microsoft.FSharpLu.Json
open Newtonsoft.Json.Serialization
// ---------------------------------
// Web app
// ---------------------------------

let webApp = routing

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder
        // .AllowAnyOrigin()
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001",
            "http://localhost:3000",
            "https://sigmachat.cc",
            "http://frontend:3000",
            "http://frontend"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler(errorHandler).UseHttpsRedirection())
        .UseRouting()
        .UseCors(configureCors)
        .UseAuthentication()
        // .UseAuthorization()
        .UseEndpoints(fun endpoints -> endpoints.MapHub<ChatHub>("/hub") |> ignore)
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    // stupid dapper config
    registerTypeHandlers () |> ignore

    services.AddTransient<IDbConnection>(fun serviceProvider ->
        // The configuration information is in appsettings.json
        let settings = serviceProvider.GetService<IConfiguration>()
        let connection = new NpgsqlConnection(settings.["DbConnectionString"])
        upcast connection)
    |> ignore

    services.AddTransient<BlobContainerClient>(fun serviceProvider ->
        let settings = serviceProvider.GetService<IConfiguration>()
        let connectionString = settings.["BlobConnectionString"]

        let blobServiceClient =
            new BlobServiceClient(
                connectionString,
                // Azurite seems to be working only with API version 2021-12-02
                new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02)
            )

        let containerClient = blobServiceClient.GetBlobContainerClient("images")
        do containerClient.CreateIfNotExists(PublicAccessType.Blob) |> ignore

        containerClient)
    |> ignore

    services.AddCors() |> ignore

    services.AddSignalR(fun conf ->
        conf.EnableDetailedErrors = Nullable true |> ignore
        conf.KeepAliveInterval = Nullable(TimeSpan.FromSeconds(5)) |> ignore
        conf.HandshakeTimeout = Nullable(TimeSpan.FromSeconds(5)) |> ignore)
    |> ignore

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun (options) ->
            // TODO unhardcode this stuff
            let domain = "https://dev-szyhz3rxdab8xgmo.us.auth0.com/"
            let audience = "SigmaChatBackend"

            options.Authority <- domain
            options.Audience <- audience

            options.TokenValidationParameters <- TokenValidationParameters(NameClaimType = ClaimTypes.NameIdentifier))
    |> ignore

    // services.AddAuthorization() |> ignore
    // => options {
    //     options.AddPolicy(
    //         "read:admin-messages",
    //         => policy { policy.Requirements.Add(new RbacRequirement("read:admin-messages")) }
    //     )
    // }


    // services.AddSingleton<IAuthorizationHandler, RbacHandler>()
    services.AddGiraffe() |> ignore

    let customSettings = JsonSerializerSettings()
    customSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    // this is for options serializing striaght to just/null values
    customSettings.Converters.Add(CompactUnionJsonConverter(true))

    services.AddSingleton<Json.ISerializer>(NewtonsoftJson.Serializer(customSettings))
    |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
