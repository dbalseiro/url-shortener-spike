module FsShortener.Shortener.Encoding

open System.Net

let private chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"

let base62Encode: int64 -> string = fun i ->
    let rec encode n acc =
        if n = 0L then acc
        else
            let remainder = int (n % 62L)
            let next = n / 62L
            encode next (chars.[remainder] :: acc)
    match i with
    | 0L -> "0"
    | _ -> System.String.Concat(encode i [])

let base62Decode: string -> int64 = fun s ->
    let charToValue c = int64 (chars.IndexOf(c: char))
    let rec decode acc = function
        | [] -> acc
        | c :: cs -> decode (acc * 62L + charToValue c) cs
    match s with
    | "" -> 0L
    | _ -> decode 0L (List.ofSeq s)

let generateCode: unit -> string = fun () ->
    let random = System.Random()
    int64 (random.Next(0, System.Int32.MaxValue))
    |> base62Encode

type ResponseHeaders = (string * string) list
let mapErrorToStatus: Domain.ShortenError -> (HttpStatusCode * ResponseHeaders * obj) = fun err ->
    match err with
    | Domain.InvalidRequest violations ->
        HttpStatusCode.BadRequest, 
        [("Content-Type", "application/json")],
        {|
            message = "Invalid request"
            violations = violations
        |}
    | Domain.AliasTaken ->
        HttpStatusCode.Conflict, 
        [("Content-Type", "text/plain")], 
        "Alias already taken"
    | Domain.RateLimited tryAgain -> 
        HttpStatusCode.TooManyRequests,
        [
            "Content-Type", "text/plain";
            "Retry-After", tryAgain.RetryAfter.ToString()
        ],
        "Rate limited"
    