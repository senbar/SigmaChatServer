module Dapper.Extensions

open System
open Dapper

let extractValue (x: obj) =
    match x with
    | null -> null
    | _ ->
        match x.GetType().GetProperty("Value") with
        | null -> x
        | prop -> prop.GetValue(x)

let (+>) (map: Map<string, obj>) (key, value) = map.Add(key, extractValue value)
let singleParam (key, value) = (Map.empty) +> (key, value)

type OptionHandler<'T>() =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override __.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override __.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(value :?> 'T)

let registerTypeHandlers () =
    SqlMapper.AddTypeHandler(OptionHandler<Guid>())
    SqlMapper.AddTypeHandler(OptionHandler<int64>())
    SqlMapper.AddTypeHandler(OptionHandler<int>())
    SqlMapper.AddTypeHandler(OptionHandler<string>())
    SqlMapper.AddTypeHandler(OptionHandler<DateTime>())
