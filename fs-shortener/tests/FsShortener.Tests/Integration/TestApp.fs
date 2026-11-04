module FsShortener.Tests.Integration.TestApp

open System.Net.Http
open System.Threading.Tasks
open System.Net.Http.Json
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.TestHost

/// We are going to replace this:
// let withClient (f: System.Net.Http.HttpClient -> Threading.Tasks.Task) = task {
//     let builder = WebApplication.CreateBuilder()
//     builder.WebHost.UseTestServer() |> ignore
//     // aquí se pueden reemplazar servicios: builder.Services.AddSingleton<LinkStore>(InMemory.create())
//     let app = App.configure builder
//     do! app.StartAsync()
//     try
//         do! f (app.GetTestClient())
//     finally
//         app.DisposeAsync().AsTask().Wait()
// }
//
// [<Fact>]
// let ``GET /health returns Healthy`` () =
//     withClient (fun client -> task {
//         let! res = client.GetAsync "/health"
//         Assert.Equal(HttpStatusCode.OK, res.StatusCode)
//         let! body = res.Content.ReadFromJsonAsync<HealthResponse>()
//         Assert.Equal("Healthy", body.status)
//     })

// This type is a Wrapper for an API call
// Basically what goes into "withClient"
type Api<'a> = HttpClient -> Task<'a>

// let's wrap all the actions taken from "withClient" into a function
let private run (test: Api<unit>) : Task = task {
    let builder = WebApplication.CreateBuilder()
    builder.WebHost.UseTestServer() |> ignore
    let app = FsShortener.App.configure builder ""
    do! app.StartAsync()
    try
        do! test (app.GetTestClient())
    finally
        app.DisposeAsync().AsTask().Wait()
}

// this is kind of a reader?
// client :: Api will be passed to the API functions automatically
type ApiBuilder() =
    // actual runner
    member _.Run(test: Api<unit>) : Task = 
        run test
    member _.Zero() : Api<unit> = 
        fun _ -> Task.FromResult()
    member _.Return(x: 'a) : Api<'a> =
        fun _ -> Task.FromResult(x)
    member _.Bind(m: Api<'a>, f: 'a -> Api<'b>) : Api<'b> =
        fun client -> task {
            let! a = m client
            return! f a client
        }
    member _.Bind(t: Task<'a>, f: 'a -> Api<'b>) : Api<'b> =
        fun client -> task {
            let! a = t
            return! f a client
        }

let withClient = ApiBuilder()

// This block
// ```
// withClient {
//     let! res = get "/health"
//     Assert.Equal(HttpStatusCode.OK, res.StatusCode)
// }
// ```
// is the same as 
// ```
// withClient.Run(
//   withClient.Bind(
//     get "/health",
//     fun res ->
//         Assert.Equal(HttpStatusCode.OK, res.StatusCode)
//         withClient.Zero()
//   )
// )
// ```

// primitives
let get (url: string) : Api<HttpResponseMessage> = 
    fun c -> c.GetAsync url

let post (url: string) (body: obj) : Api<HttpResponseMessage> =
    fun c -> c.PostAsJsonAsync(url, body)