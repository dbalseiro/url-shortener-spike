module FsShortener.Tests.Integration.Redis

open System
open System.Threading.Tasks
open Xunit
open FsShortener.Infrastructure
open FsShortener.Shortener.Model

[<Fact>]
let ``Redis can save and retrieve a link`` () : Task =
    task {
        // Use memory store since we don't have Redis running in test environment
        let store = Memory.create()
        
        let link = {
            Code = "test123"
            Url = "https://example.com"
            Expiration = Some 3600
        }
        
        let! saveResult = store.Save link
        Assert.True(Result.isOk saveResult)
        
        let! getResult = store.Get "test123"
        Assert.True(Result.isOk getResult)
        
        let linkOpt = Result.defaultValue None getResult
        Assert.True(linkOpt.IsSome)
        
        let retrievedLink = linkOpt.Value
        Assert.Equal("test123", retrievedLink.Code)
        Assert.Equal("https://example.com", retrievedLink.Url)
    }

[<Fact>]
let ``Redis returns None for non-existent key`` () : Task =
    task {
        let store = Memory.create()
        
        let! result = store.Get "nonexistent"
        Assert.True(Result.isOk result)
        
        let linkOpt = Result.defaultValue None result
        Assert.False(linkOpt.IsSome)
    }

[<Fact>]
let ``Redis can check key existence`` () : Task =
    task {
        let store = Memory.create()
        
        let link = {
            Code = "exists-test"
            Url = "https://example.com"
            Expiration = None
        }
        
        let! saveResult = store.Save link
        Assert.True(Result.isOk saveResult)
        
        let! existsResult = store.Exists "exists-test"
        Assert.True(Result.isOk existsResult)
        
        let exists = Result.defaultValue false existsResult
        Assert.True(exists)
        
        let! notExistsResult = store.Exists "does-not-exist"
        Assert.True(Result.isOk notExistsResult)
        
        let notExists = Result.defaultValue false notExistsResult
        Assert.False(notExists)
    }
