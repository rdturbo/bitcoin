#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

let N = fsi.CommandLineArgs.[1] |>int64

// let timer = Diagnostics.Stopwatch.StartNew()
let system = ActorSystem.Create("FSharp")
let random' = Random()
type MessagesOfActor = 
    | DispatcherInput of int64
    | Finished of string
    | FailFinished of string * int64
    | MessageToWorker of int64
    | Hash of string

let workerIds = new HashSet<int>()
let badstrs = new ConcurrentDictionary<string, int>()
badstrs.TryAdd("aaa", 0) |> ignore
for i in [1..8] do
    workerIds.Add(i) |> ignore
let mutable leading = ""
let byteConvertToString x = 
    BitConverter.ToString(x).Replace("-", "").ToLower()

let stringConvertToByte (str: string) = 
    System.Text.Encoding.ASCII.GetBytes(str)

let randomStr = 
            let chars = "abcdefghijklmnopqrstuvwxyz0123456789"
            let charsLen = chars.Length
            fun len -> 
                let randomChars = [|for i in 0..len -> chars.[random'.Next(charsLen)]|]
                String(randomChars)

let mutable countResponce = 0L 
let worker(mailbox: Actor<_>) = 
                let rec loop() = actor {
                        let! message = mailbox.Receive()
                        let sender = mailbox.Sender()
                        match message with
                        | MessageToWorker(msg) -> 
                            let zeroesString = msg |>int
                            let zeroes = "0"
                            let multiply text times = String.replicate times text
                            leading <- multiply zeroes zeroesString
                            // printfn "I am an actor and I have this message %s length of %d" msg msg.Length
                            let mutable continueLooping = true
                            let mutable loopCount = 0
                            while continueLooping do
                                let mutable newStrNotFound = true
                                let mutable randomString9 = ""
                                let mutable countRandTrys = 0 
                                while newStrNotFound do
                                    randomString9 <- randomStr(4)
                                    if not <| badstrs.ContainsKey(randomString9) then
                                        newStrNotFound <- false
                                    else 
                                        countRandTrys <- countRandTrys + 1
                                    if countRandTrys > 5 then
                                        continueLooping <- false
                                        sender <! FailFinished("Couldn't find one...", msg)
                                let perName = "rupayandas"
                                let strcon = perName + ";" + randomString9                   
                                let hashval = strcon  
                                                |> stringConvertToByte
                                                |> HashAlgorithm.Create("SHA256").ComputeHash
                                                |> byteConvertToString
                                let num = leading.Length |>int
                                let hashvalBegin = hashval.[0..(num - 1)]
                                if hashvalBegin = leading then
                                    continueLooping <- false
                                    let output = sprintf "\n%s %s\n" strcon hashval
                                    sender <! Finished(output)
                                else
                                    badstrs.TryAdd(randomString9, 0) |> ignore
                                    if loopCount < 2 then
                                        continueLooping <- true
                                        loopCount <- loopCount + 1
                                    else
                                        continueLooping <- false
                                        sender <! FailFinished("Couldn't find one...", msg)
                        | _ ->  failwith "unknown message"
                        return! loop()
                    }
                loop()

let Dispatcher(mailbox: Actor<_>) = 
            let rec loop() = actor {
                    mailbox.Context.SetReceiveTimeout(TimeSpan.FromSeconds 10000.0)
                    let! message = mailbox.Receive()
                    match message with
                    | DispatcherInput(n) ->
                        let actorWorkerList = 
                            [1L .. 8L]
                            |> List.map(fun i -> spawn system (sprintf "Worker_%d" i) worker)
                        
                        for j in 0L .. 7L do
                            actorWorkerList.Item(j |> int) <! MessageToWorker(n)
                    | Finished(output) ->
                        countResponce <- countResponce + 1L
                        if countResponce <= 1L then
                            printfn "%s"output
                            if countResponce = 1L then
                                mailbox.Context.System.Terminate() |> ignore
                    | FailFinished(failStr, n) ->
                        let mutable newNumberNotFound = true
                        let mutable workerId = 0
                        while newNumberNotFound do
                            workerId <- random'.Next(500, 1000000000)
                            if workerIds.Add(workerId) then
                                newNumberNotFound <- false
                        let workerName = sprintf "Worker_%d" (workerId)
                        let curWorker = spawn system workerName worker
                        curWorker <! MessageToWorker(n) 
                    | _ -> printfn "An error occured"
                    return! loop()
                }
            loop()

let Boss  = spawn system "Boss" Dispatcher


printfn "%d" N 
Boss <! DispatcherInput(N)
system.WhenTerminated.Wait()
//system.Terminate() 