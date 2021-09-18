open System
open System.Security.Cryptography

let timer = Diagnostics.Stopwatch.StartNew()

let random' = Random()

let byteConvertToString x = 
    BitConverter.ToString(x).Replace("-", "").ToLower()

let stringConvertToByte (str: string) = 
    System.Text.Encoding.ASCII.GetBytes(str)

let mutable continueLooping = true

let mine num=
    while continueLooping do

        let randomStr = 
            let chars = "abcdefghijklmnopqrstuvwxyz0123456789"
            let charsLen = chars.Length
            

            fun len -> 
                let randomChars = [|for i in 0..len -> chars.[random'.Next(charsLen)]|]
                String(randomChars)

        let randomString9 = randomStr(9)

        let perName = "rupayandas"
        let msg = perName + ";" + randomString9                   

        let hashval = msg 
                        |> stringConvertToByte
                        |> HashAlgorithm.Create("SHA256").ComputeHash
                        |> byteConvertToString
        
        let str2 = (hashval.[0..3])
        if str2 = num then 
            printfn $"{msg}\t{hashval}"
            continueLooping <- false

mine("0000")

timer.Stop()
printfn $"{timer.Elapsed.TotalMilliseconds}"