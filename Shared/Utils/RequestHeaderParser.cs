using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Utils
{
    public static class RequestHeaderParser
    {
        public static (long StartPosition, long EndPosition) GetHeaderRangeParsedValues(this HttpRequest request, int defaultEndSize)
        {
            //const long ChunkSize = 1024 * 1024 * 4;
            var range = request.Headers["Range"].ToString();

            var dashIndex = range.IndexOf('-');
            var startPosition = long.Parse(range.Substring(6, dashIndex - 6).ToString());
            var endPosition = startPosition + defaultEndSize;
            var endRangeString = range.Substring(dashIndex + 1);
            if (!string.IsNullOrWhiteSpace(endRangeString))
            {
                endPosition = long.Parse(endRangeString);
            }
            return (startPosition, endPosition);
        }
    }
}
