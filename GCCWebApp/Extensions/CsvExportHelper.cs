using System.Text;

namespace GCCWebApp.Extensions
{
    /// <summary>
    /// Helpers for emitting CSV downloads that open correctly in Excel on Windows.
    ///
    /// Excel on Windows decodes a CSV that has no byte-order mark as Windows-1252, not UTF-8.
    /// Any non-ASCII character then renders as mojibake — e.g. a right single quote ' (U+2019,
    /// UTF-8 bytes E2 80 99) shows up as "â€™", and an em dash — (U+2014) as "â€"".  mojibake-doc
    /// Prepending the UTF-8 BOM tells Excel to decode the file as UTF-8 so the text displays
    /// correctly.
    ///
    /// Mirrors the BOM fix already applied to the certificate import template
    /// (CertificateImportController.DownloadTemplate). Use this for analyst/browser-facing CSV
    /// downloads. Do NOT use it on the /api/v1/certificates* partner export path — those bytes
    /// are a frozen contract and must not change without partner sign-off.
    /// </summary>
    public static class CsvExportHelper
    {
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        /// <summary>Encodes CSV text as UTF-8 bytes with a leading BOM.</summary>
        public static byte[] ToCsvBytesWithBom(string csv)
        {
            var preamble = Utf8WithBom.GetPreamble();
            var body = Utf8WithBom.GetBytes(csv ?? string.Empty);
            return Concat(preamble, body);
        }

        /// <summary>
        /// Prepends a UTF-8 BOM to CSV bytes that were already UTF-8 encoded (e.g. returned by a
        /// service). If the BOM is already present the input is returned unchanged.
        /// </summary>
        public static byte[] WithBom(byte[] utf8CsvBytes)
        {
            var preamble = Utf8WithBom.GetPreamble();
            if (utf8CsvBytes == null)
            {
                return preamble;
            }
            if (utf8CsvBytes.Length >= 3 &&
                utf8CsvBytes[0] == preamble[0] &&
                utf8CsvBytes[1] == preamble[1] &&
                utf8CsvBytes[2] == preamble[2])
            {
                return utf8CsvBytes; // already has a BOM
            }
            return Concat(preamble, utf8CsvBytes);
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }
    }
}
