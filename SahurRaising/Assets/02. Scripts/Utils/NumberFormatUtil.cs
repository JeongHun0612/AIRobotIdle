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

        /// <summary>
        /// 정수를 로마자로 변환합니다 (1~3999 범위)
        /// </summary>
        /// <param name="number">변환할 숫자</param>
        /// <returns>로마자 문자열 (범위를 벗어나면 빈 문자열 반환)</returns>
        public static string ToRomanNumeral(int number)
        {
            if (number <= 0 || number > 3999)
                return string.Empty;

            string[] thousands = { "", "M", "MM", "MMM" };
            string[] hundreds = { "", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM" };
            string[] tens = { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" };
            string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

            return thousands[number / 1000] +
                   hundreds[(number % 1000) / 100] +
                   tens[(number % 100) / 10] +
                   ones[number % 10];
        }

        /// <summary>
        /// long 타입 숫자를 로마자로 변환합니다 (1~3999 범위)
        /// </summary>
        public static string ToRomanNumeral(long number)
        {
            if (number <= 0 || number > 3999)
                return string.Empty;

            return ToRomanNumeral((int)number);
        }

        /// <summary>
        /// BigDouble을 로마자로 변환합니다 (1~3999 범위의 정수만 지원)
        /// </summary>
        public static string ToRomanNumeral(BigDouble number)
        {
            if (number <= 0 || number > 3999)
                return string.Empty;

            // 정수인지 확인
            double doubleValue = number.ToDouble();
            if (System.Math.Abs(doubleValue % 1) >= double.Epsilon)
                return string.Empty;

            return ToRomanNumeral((int)doubleValue);
        }
    }
}