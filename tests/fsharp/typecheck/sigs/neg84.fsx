module M

let f (xs:string list) = 
   match xs  with 
   | [] -> 
        xs 
        |> List.map (fun x -> x 
        //|> List.map (fun x -> x + "a") 
        //|> List.map (fun x -> x + "b")

