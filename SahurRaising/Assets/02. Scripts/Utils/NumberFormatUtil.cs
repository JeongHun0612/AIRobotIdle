using BreakInfinity;

namespace SahurRaising.Utils
{
    public static class NumberFormatUtil
    {
        /// <summary>
        /// BigDouble을 1000단위 알파벳 표기법으로 포맷팅합니다 (예: 1.2A, 1.5B, 1.3C 등)
        /// </summary>
        /// <param name="value">포맷팅할 값</param>
        /// <param name="decimalPlaces">소수점 자릿수 (기본값: 2)</param>
        /// <returns>포맷팅된 문자열</returns>
        public static string FormatBigDouble(BigDouble value, int decimalPlaces = 2)
        {
            if (value <= 0)
                return "0";

            // 1000 미만은 일반 숫자로 표시
            if (value < 1000)
            {
                // 소수점이 없으면 정수로, 있으면 decimalPlaces만큼 표시
                double doubleValue = value.ToDouble();
                if (System.Math.Abs(doubleValue % 1) < double.Epsilon)
                {
                    return ((long)doubleValue).ToString();
                }
                return doubleValue.ToString($"F{decimalPlaces}");
            }

            // Exponent를 이용해 1000의 몇 제곱인지 계산
            int exponent = (int)value.Exponent;
            int suffixIndex = (exponent - 3) / 3;

            // exponent가 3 미만이면 0으로 처리
            if (exponent < 3)
            {
                suffixIndex = 0;
            }

            // 알파벳 배열 (A부터 시작)
            string[] suffixes = {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
            };

            // Mantissa와 나머지 exponent를 이용해 표시할 값 계산
            double valueMantissa = value.Mantissa;
            // exponent % 3이 0, 1, 2일 수 있으므로, 이를 고려해서 계산
            int remainder = exponent % 3;
            double resultValue = valueMantissa * System.Math.Pow(10, remainder);

            // 소수점이 있는지 확인
            bool hasDecimal = System.Math.Abs(resultValue % 1) >= double.Epsilon;

            // 포맷 문자열 생성
            string formatString = hasDecimal ? $"F{decimalPlaces}" : "F0";
            string formattedNumber = resultValue.ToString(formatString);

            // 소수점이 없으면 .0 제거
            if (!hasDecimal && formattedNumber.Contains("."))
            {
                formattedNumber = formattedNumber.Split('.')[0];
            }

            // Z를 넘어가면 AA, AB, AC... 형식으로 확장
            if (suffixIndex >= suffixes.Length)
            {
                // AA, AB, AC... 형식으로 확장
                int firstLetterIndex = (suffixIndex - suffixes.Length) / 26;
                int secondLetterIndex = (suffixIndex - suffixes.Length) % 26;

                if (firstLetterIndex < suffixes.Length)
                {
                    string suffix = suffixes[firstLetterIndex] + suffixes[secondLetterIndex];
                    return $"{formattedNumber}{suffix}";
                }
                else
                {
                    // 매우 큰 수는 과학적 표기법 사용
                    return value.ToString("G3");
                }
            }

            return $"{formattedNumber}{suffixes[suffixIndex]}";
        }
    }
}