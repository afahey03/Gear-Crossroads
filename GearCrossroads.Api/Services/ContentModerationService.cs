using System.Text.RegularExpressions;

namespace GearCrossroads.Api.Services
{
    public interface IContentModerationService
    {
        Task<(bool IsClean, List<string> ViolationReasons)> ModerateTextAsync(string text);
    }

    public class ContentModerationService : IContentModerationService
    {
        private readonly ILogger<ContentModerationService> _logger;
        private readonly HashSet<string> _bannedWords;
        private readonly List<string> _bannedPatterns;

        public ContentModerationService(ILogger<ContentModerationService> logger)
        {
            _logger = logger;

            // Explicit banned words (add more as needed)
            _bannedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Profanity
                "fuck", "shit", "bitch", "ass", "damn", "hell", "crap", "piss",
                "bastard", "dick", "cock", "pussy", "cunt", "slut", "whore",
                
                // Slurs and hate speech (partial list - add more as needed)
                "nigger", "nigga", "faggot", "fag", "retard", "tranny",
                "chink", "spic", "kike", "wetback", "towelhead",
                
                // Sexual content
                "porn", "xxx", "sex", "nude", "naked", "hentai", "nsfw",
                "rape", "molest", "pedophile", "pedo",
                
                // Violence/illegal
                "kill", "murder", "bomb", "terrorist", "nazi", "kkk",
                "drug", "cocaine", "heroin", "meth", "weed",
                
                // Common bypass attempts
                "n1gger", "f4ggot", "fvck", "sh1t", "b1tch", "p0rn"
            };

            // Regex patterns to catch leetspeak and character substitutions
            _bannedPatterns = new List<string>
            {
                @"n[i1!|l][g9][g9][e3][r4]",          // n*gger variations
                @"f[a4@][g9][g9]?[o0][t7]",          // f*ggot variations  
                @"f[u\*]c?k",                         // f*ck variations
                @"[s5$][h#][i1!][t7]",               // sh*t variations
                @"b[i1!][t7][c¢][h#]",               // b*tch variations
                @"p[o0][r4][n#]",                    // p*rn variations
                @"[s5$][e3][x\*]",                   // s*x variations
                @"n[a4@][z2][i1!]",                  // n*zi variations
                @"k[i1!]ll",                         // k*ll variations
                @"r[a4@]p[e3]",                      // r*pe variations
            };
        }

        public Task<(bool IsClean, List<string> ViolationReasons)> ModerateTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult((true, new List<string>()));
            }

            var violations = new List<string>();
            var normalizedText = text.ToLowerInvariant();

            // Remove spaces and special characters for bypass detection
            var compactText = Regex.Replace(normalizedText, @"[\s\-_\.\*]", "");

            // Check for exact word matches
            var words = Regex.Split(normalizedText, @"\W+");
            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word)) continue;

                if (_bannedWords.Contains(word))
                {
                    violations.Add($"Inappropriate language detected");
                    _logger.LogWarning("[ContentModeration] Banned word detected: {Word}", word);
                }
            }

            // Check for pattern matches (leetspeak, character substitution)
            foreach (var pattern in _bannedPatterns)
            {
                if (Regex.IsMatch(compactText, pattern, RegexOptions.IgnoreCase))
                {
                    violations.Add($"Inappropriate content detected (pattern match)");
                    _logger.LogWarning("[ContentModeration] Banned pattern detected in text");
                    break; // Only report once per pattern type
                }
            }

            // Check for repeated characters (e.g., "ffffffffuck" to bypass filters)
            var deduplicatedText = Regex.Replace(compactText, @"(.)\1{2,}", "$1$1");
            var deduplicatedWords = Regex.Split(deduplicatedText, @"\W+");
            foreach (var word in deduplicatedWords)
            {
                if (_bannedWords.Contains(word))
                {
                    violations.Add($"Inappropriate language detected (character repetition bypass)");
                    _logger.LogWarning("[ContentModeration] Banned word detected with character repetition");
                }
            }

            // Check for all caps aggressive language
            if (text.Length > 10 && text == text.ToUpperInvariant())
            {
                var capsWords = Regex.Split(text, @"\W+");
                foreach (var word in capsWords)
                {
                    if (_bannedWords.Contains(word.ToLowerInvariant()))
                    {
                        violations.Add($"Inappropriate language detected");
                    }
                }
            }

            var isClean = violations.Count == 0;

            if (!isClean)
            {
                _logger.LogWarning("[ContentModeration] Content moderation failed. Violations: {Count}", violations.Count);
            }

            return Task.FromResult((isClean, violations.Distinct().ToList()));
        }
    }
}
