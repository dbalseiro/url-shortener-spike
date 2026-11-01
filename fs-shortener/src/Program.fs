open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =
    let app = 
        WebApplication.CreateBuilder(args)
        |> FsShortener.App.configure
    app.Run()
    0 // Exit code
