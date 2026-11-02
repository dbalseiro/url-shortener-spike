module FsShortener.Shortener.Validation

open FsShortener.Shortener.Domain

let validUrl : string -> string option = fun url ->
    match System.Uri.TryCreate(url, System.UriKind.Absolute) with
    | true, uri when uri.Scheme = System.Uri.UriSchemeHttp || uri.Scheme = System.Uri.UriSchemeHttps -> None
    | true, _ -> Some "Schema not supported - only HTTP and HTTPS are allowed"
    | false, _ -> Some $"Invalid URL {url}"

let validAlias : string -> bool = fun alias ->
    not (System.String.IsNullOrWhiteSpace alias) 
    && System.Text.RegularExpressions.Regex.IsMatch(alias, "^[a-zA-Z0-9]+$") 

let validTTL : int -> bool = fun ttl -> ttl > 0

let private boolToSome : 'a -> bool -> 'a option = fun v b -> 
    if b then None else Some v

let validateRequest : ShortenRequest -> Violation list = fun req ->
    let violations = [
        validUrl req.Url |> Option.map InvalidUrl
        req.TTL |> Option.bind (boolToSome InvalidTTL << validTTL)
        req.Alias |> Option.bind (boolToSome InvalidAlias << validAlias)
    ]
    List.choose id violations
    