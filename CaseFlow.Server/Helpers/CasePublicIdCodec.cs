using System.Security.Cryptography;
using System.Text;

namespace CaseFlow.Server.Helpers;

public static class CasePublicIdCodec
{
    private const string Base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int SignatureLength = 8;

    public static string Encode(int caseId, string salt)
    {
        if (caseId <= 0) throw new ArgumentOutOfRangeException(nameof(caseId));
        var payload = ToBase62(caseId);
        var signature = ComputeSignature(payload, salt);
        return payload + signature;
    }

    public static bool TryDecode(string token, string salt, out int caseId)
    {
        caseId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;

        token = token.Trim();
        if (token.Length <= SignatureLength) return false;

        var payload = token[..^SignatureLength];
        var signature = token[^SignatureLength..];
        var expected = ComputeSignature(payload, salt);

        var signatureBytes = Encoding.ASCII.GetBytes(signature.ToLowerInvariant());
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        if (!CryptographicOperations.FixedTimeEquals(signatureBytes, expectedBytes)) return false;

        return TryFromBase62(payload, out caseId) && caseId > 0;
    }

    private static string ComputeSignature(string payload, string salt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant()[..SignatureLength];
    }

    private static string ToBase62(int value)
    {
        var current = (long)value;
        var chars = new List<char>();
        while (current > 0)
        {
            var index = (int)(current % 62);
            chars.Add(Base62Alphabet[index]);
            current /= 62;
        }

        chars.Reverse();
        return new string(chars.ToArray());
    }

    private static bool TryFromBase62(string input, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        long result = 0;
        foreach (var ch in input)
        {
            var index = Base62Alphabet.IndexOf(ch);
            if (index < 0) return false;

            checked
            {
                result = result * 62 + index;
            }
        }

        if (result <= 0 || result > int.MaxValue) return false;
        value = (int)result;
        return true;
    }
}
