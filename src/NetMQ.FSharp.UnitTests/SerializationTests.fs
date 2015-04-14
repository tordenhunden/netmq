module SerializationTests

open Xunit
open FsUnit.Xunit
open FsCheck
open FsCheck.Xunit
open NetMQ.PubSub.Transport
open System.Collections.Generic
open System.Linq
open NetMQ.PubSub.Json

let dicEquals (d1:Dictionary<string,string>, d2:Dictionary<string,string>) =
    let sort (d:Dictionary<string,string>) = 
        d |> Seq.toList
          |> List.sortBy (fun x -> x.Key)
    List.zip (sort d1) (sort d2) |> List.map (fun (x1,x2) -> x1.Key = x2.Key && x1.Value = x2.Value) 
                                 |> List.fold (fun x a -> x && a) true


let tmEquals (m1:TransportMessage, m2:TransportMessage) =
    (List.ofArray m1.Body = List.ofArray m2.Body 
    && dicEquals(m1.Headers,m2.Headers) 
    && m1.SequenceNumber = m2.SequenceNumber 
    && m1.Topic = m2.Topic) 
    |@ sprintf "[body: %A], [headers: %A], m1:%A ,m2:%A]" 
               (List.ofArray m1.Body = List.ofArray m2.Body) 
               (dicEquals (m1.Headers,m2.Headers))
               m1
               m2

//[<Fact>]
[<Property(MaxTest = 10)>]
let ``serialize o deserialize   is identity`` () =
    Check.QuickThrowOnFailure 
        (fun (topic:NonNull<string>) seqNo headers body -> let m = new TransportMessage (topic.Get,seqNo,headers,body)
                                                           let zm = new NetMQ.NetMQMessage()
                                                           JsonSerialization.WriteTransportMessage (zm, m)
                                                           let m' = JsonSerialization.ReadTransportMessage zm
                                                           tmEquals (m,m'))
                                            
