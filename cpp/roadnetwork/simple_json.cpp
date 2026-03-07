#include "simple_json.hpp"

#include <cstdlib>
#include <limits>

namespace simple_json
{
    namespace
    {
        struct Parser
        {
            const std::string& text;
            size_t pos = 0;
            std::string error;

            explicit Parser(const std::string& t) : text(t) {}

            ParseResult MakeError(const std::string& msg)
            {
                return ParseResult{std::nullopt, msg};
            }

            void SkipWhitespace()
            {
                while (pos < text.size() && std::isspace(static_cast<unsigned char>(text[pos])))
                {
                    ++pos;
                }
            }

            bool Consume(char expected)
            {
                if (pos < text.size() && text[pos] == expected)
                {
                    ++pos;
                    return true;
                }
                return false;
            }

            ParseResult ParseValue()
            {
                SkipWhitespace();
                if (pos >= text.size())
                {
                    return MakeError("Unexpected end of input");
                }
                char c = text[pos];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == 't' || c == 'f') return ParseBool();
                if (c == 'n') return ParseNull();
                if (c == '-' || std::isdigit(static_cast<unsigned char>(c))) return ParseNumber();
                return MakeError("Unexpected character");
            }

            ParseResult ParseObject()
            {
                if (!Consume('{')) return MakeError("Expected '{'");
                SkipWhitespace();
                std::unordered_map<std::string, JsonValue> obj;
                if (Consume('}'))
                {
                    return ParseResult{JsonValue::Object(obj), ""};
                }
                while (true)
                {
                    auto keyRes = ParseString();
                    if (!keyRes.value) return keyRes;
                    std::string key = keyRes.value->string;
                    SkipWhitespace();
                    if (!Consume(':')) return MakeError("Expected ':'");
                    auto valRes = ParseValue();
                    if (!valRes.value) return valRes;
                    obj.emplace(std::move(key), std::move(*valRes.value));
                    SkipWhitespace();
                    if (Consume('}'))
                    {
                        break;
                    }
                    if (!Consume(',')) return MakeError("Expected ',' or '}'");
                    SkipWhitespace();
                }
                return ParseResult{JsonValue::Object(obj), ""};
            }

            ParseResult ParseArray()
            {
                if (!Consume('[')) return MakeError("Expected '['");
                SkipWhitespace();
                std::vector<JsonValue> arr;
                if (Consume(']'))
                {
                    return ParseResult{JsonValue::Array(arr), ""};
                }
                while (true)
                {
                    auto valRes = ParseValue();
                    if (!valRes.value) return valRes;
                    arr.push_back(std::move(*valRes.value));
                    SkipWhitespace();
                    if (Consume(']'))
                    {
                        break;
                    }
                    if (!Consume(',')) return MakeError("Expected ',' or ']'");
                    SkipWhitespace();
                }
                return ParseResult{JsonValue::Array(arr), ""};
            }

            ParseResult ParseString()
            {
                if (!Consume('"')) return MakeError("Expected '\"'");
                std::string result;
                while (pos < text.size())
                {
                    char c = text[pos++];
                    if (c == '"')
                    {
                        return ParseResult{JsonValue::String(result), ""};
                    }
                    if (c == '\\')
                    {
                        if (pos >= text.size()) return MakeError("Incomplete escape sequence");
                        char esc = text[pos++];
                        switch (esc)
                        {
                            case '"': result.push_back('"'); break;
                            case '\\': result.push_back('\\'); break;
                            case '/': result.push_back('/'); break;
                            case 'b': result.push_back('\b'); break;
                            case 'f': result.push_back('\f'); break;
                            case 'n': result.push_back('\n'); break;
                            case 'r': result.push_back('\r'); break;
                            case 't': result.push_back('\t'); break;
                            case 'u':
                                // Minimal support: skip \uXXXX
                                if (pos + 4 > text.size()) return MakeError("Incomplete unicode escape");
                                pos += 4;
                                result.push_back('?');
                                break;
                            default:
                                return MakeError("Invalid escape sequence");
                        }
                    }
                    else
                    {
                        result.push_back(c);
                    }
                }
                return MakeError("Unterminated string");
            }

            ParseResult ParseBool()
            {
                if (text.compare(pos, 4, "true") == 0)
                {
                    pos += 4;
                    return ParseResult{JsonValue::Bool(true), ""};
                }
                if (text.compare(pos, 5, "false") == 0)
                {
                    pos += 5;
                    return ParseResult{JsonValue::Bool(false), ""};
                }
                return MakeError("Invalid boolean");
            }

            ParseResult ParseNull()
            {
                if (text.compare(pos, 4, "null") == 0)
                {
                    pos += 4;
                    return ParseResult{JsonValue::Null(), ""};
                }
                return MakeError("Invalid null");
            }

            ParseResult ParseNumber()
            {
                size_t start = pos;
                if (text[pos] == '-') ++pos;
                while (pos < text.size() && std::isdigit(static_cast<unsigned char>(text[pos]))) ++pos;
                if (pos < text.size() && text[pos] == '.')
                {
                    ++pos;
                    while (pos < text.size() && std::isdigit(static_cast<unsigned char>(text[pos]))) ++pos;
                }
                if (pos < text.size() && (text[pos] == 'e' || text[pos] == 'E'))
                {
                    ++pos;
                    if (pos < text.size() && (text[pos] == '+' || text[pos] == '-')) ++pos;
                    while (pos < text.size() && std::isdigit(static_cast<unsigned char>(text[pos]))) ++pos;
                }
                double value = std::strtod(text.c_str() + start, nullptr);
                return ParseResult{JsonValue::Number(value), ""};
            }
        };
    } // namespace

    ParseResult Parse(const std::string& text)
    {
        Parser parser(text);
        auto result = parser.ParseValue();
        if (!result.value)
        {
            return result;
        }
        parser.SkipWhitespace();
        if (parser.pos != text.size())
        {
            return ParseResult{std::nullopt, "Trailing characters after JSON value"};
        }
        return result;
    }
} // namespace simple_json

