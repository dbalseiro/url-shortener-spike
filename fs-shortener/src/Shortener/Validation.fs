module FsShortener.Shortener.Validation

let validUrl(url: string): string option =
    match System.Uri.TryCreate(url, System.UriKind.Absolute) with
    | true, uri when uri.Scheme = System.Uri.UriSchemeHttp || uri.Scheme = System.Uri.UriSchemeHttps -> None
    | true, _ -> Some "Schema not supported - only HTTP and HTTPS are allowed"
    | false, _ -> Some $"Invalid URL {url}"

let validAlias(alias: string): bool =
    not (System.String.IsNullOrWhiteSpace alias)
    && System.Text.RegularExpressions.Regex.IsMatch(alias, "^[a-zA-Z0-9]+$")

let validTTL : int -> bool =
    fun ttl -> ttl > 0

let private boolToSome (v: 'a) (b: bool): 'a option =
    if b then None else Some v

let validateRequest : Domain.ShortenRequest -> Domain.Violation list = fun req ->
    let violations = [
        validUrl req.Url |> Option.map Domain.InvalidUrl
        req.TTL |> Option.bind (boolToSome Domain.InvalidTTL << validTTL)
        req.Alias |> Option.bind (boolToSome Domain.InvalidAlias << validAlias)
    ]
    List.choose id violations
