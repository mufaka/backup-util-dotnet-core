using System;
using System.Collections.Generic;
using System.IO;

namespace BackupUtilityCore.YAML
{
    /// <summary>
    /// Simple YAML Parser
    /// (Not full YAML spec implementation, just relevant to config.)
    /// </summary>
    public static class YamlParser
    {
        /// <summary>
        /// Parses YAML file into Dictionary of keys/values.
        /// </summary>
        /// <param name="path">Name of YAML file</param>
        /// <returns>Key/Value pairs from parsed file.</returns>
        public static Dictionary<string, object> ParseFile(string path)
        {
            // Open file for reading
            using StreamReader streamReader = new(path);
            return ParseFromStream(streamReader);
        }

        /// <summary>
        /// Parses YAML string into Dictionary of keys/values.
        /// </summary>
        /// <param name="path">The YAML as a string</param>
        /// <returns>Key/Value pairs from parsed string.</returns>
        public static Dictionary<string, object> ParseString(string yaml)
        {
            using (var stringStream = GenerateStreamFromString(yaml))
            {
                using StreamReader streamReader = new(stringStream);
                return ParseFromStream(streamReader);
            }
        }

        private static Dictionary<string, object> ParseFromStream(StreamReader streamReader)
        {
            Dictionary<string, object> keyValues = new();

            string? line;
            string? currentKey = null;
            List<string>? currentSequence = null;

            // Read each line
            while ((line = streamReader.ReadLine()) != null)
            {
                line = TrimWhiteSpaceChars(line);

                // Check whether to ignore line
                if (IsIgnoreLine(line))
                {
                    continue;
                }
                else if (IsSequenceEntry(line))
                {
                    // Check sequence started/expected
                    if (currentSequence != null)
                    {
                        // Remove sequence char and quotes around directories
                        line = TrimSequenceChars(line);

                        // Add to sequence
                        currentSequence.Add(line);
                    }
                    else
                    {
                        // No sequence started
                        throw new FormatException($"YAML sequence entry without key definition: {line}");
                    }
                }
                else if (TryGetKeyValue(line, out string key, out string val))
                {
                    // Check whether value was found
                    if (string.IsNullOrEmpty(val))
                    {
                        // Start new sequence
                        currentKey = key;
                        currentSequence = new List<string>();

                        // Add to dictionary
                        keyValues.Add(currentKey, currentSequence);
                    }
                    else
                    {
                        // Check for an empty list
                        if (val.Replace(" ", "") == "[]")
                        {
                            // Add empty list to dictionary
                            keyValues.Add(key, new List<string>());
                        }
                        else
                        {
                            // New key/value pair
                            keyValues.Add(key, val);
                        }

                        // End any current sequence
                        currentKey = null;
                        currentSequence = null;
                    }
                }
                else
                {
                    // Not expecting this type of entry
                    throw new NotSupportedException($"YAML entry not supported: {line}");
                }
            }

            return keyValues;
        }

        /// <summary>
        /// Trims unwanted whitespace/line chars etc
        /// </summary>
        public static string TrimWhiteSpaceChars(string s)
        {
            return s.Trim(' ', '\n', '\r');
        }

        /// <summary>
        /// Trims sequence char and quotes
        /// </summary>
        public static string TrimSequenceChars(string s)
        {
            return s.TrimStart('-').Trim(' ', '\"', '\'');
        }

        /// <summary>
        /// Checks for empty line/doc start/doc end/comment
        /// </summary>
        public static bool IsIgnoreLine(string line)
        {
            return string.IsNullOrEmpty(line) || line.StartsWith("---") || line.StartsWith("...") || line.StartsWith("#");
        }

        /// <summary>
        /// Determines whether line is a sequence entry.
        /// </summary>
        public static bool IsSequenceEntry(string line)
        {
            return line.StartsWith('-') && !line.StartsWith("--");
        }

        /// <summary>
        /// Attempts to find key/value delim within line.
        /// </summary>
        /// <param name="line">string to parse</param>
        /// <param name="key">key</param>
        /// <param name="val">value (if exists - may be start of sequence)</param>
        /// <returns>true if line contains key/value delim</returns>
        public static bool TryGetKeyValue(string line, out string key, out string val)
        {
            // Check for key/value
            // (Don't use split - value may contain delim)
            int index = line.IndexOf(':');

            // Check whether delim was found
            if (index > -1)
            {
                // Trim any spaces either side of ':'
                // Convert keys to lowercase for comparison.
                key = line[..index].Trim().ToLower();
                val = line[(index + 1)..].Trim();

                return true;
            }
            else
            {
                // Responsible for initializing
                key = "";
                val = "";

                return false;
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
