#pragma once

#include <cctype>
#include <optional>
#include <string>
#include <unordered_map>
#include <vector>

namespace simple_json
{
    struct JsonValue
    {
        enum class Type
        {
            Null,
            Bool,
            Number,
            String,
            Array,
            Object
        };

        Type type = Type::Null;
        double number = 0.0;
        bool boolean = false;
        std::string string;
        std::vector<JsonValue> array;
        std::unordered_map<std::string, JsonValue> object;

        static JsonValue Null()
        {
            JsonValue v;
            v.type = Type::Null;
            return v;
        }
        static JsonValue Bool(bool b)
        {
            JsonValue v;
            v.type = Type::Bool;
            v.boolean = b;
            return v;
        }
        static JsonValue Number(double n)
        {
            JsonValue v;
            v.type = Type::Number;
            v.number = n;
            return v;
        }
        static JsonValue String(const std::string& s)
        {
            JsonValue v;
            v.type = Type::String;
            v.string = s;
            return v;
        }
        static JsonValue Array(const std::vector<JsonValue>& a)
        {
            JsonValue v;
            v.type = Type::Array;
            v.array = a;
            return v;
        }
        static JsonValue Object(const std::unordered_map<std::string, JsonValue>& o)
        {
            JsonValue v;
            v.type = Type::Object;
            v.object = o;
            return v;
        }

        bool IsNull() const { return type == Type::Null; }
        bool IsBool() const { return type == Type::Bool; }
        bool IsNumber() const { return type == Type::Number; }
        bool IsString() const { return type == Type::String; }
        bool IsArray() const { return type == Type::Array; }
        bool IsObject() const { return type == Type::Object; }

        const JsonValue* Find(const std::string& key) const
        {
            auto it = object.find(key);
            return it == object.end() ? nullptr : &it->second;
        }
    };

    struct ParseResult
    {
        std::optional<JsonValue> value;
        std::string error;
    };

    ParseResult Parse(const std::string& text);
} // namespace simple_json
