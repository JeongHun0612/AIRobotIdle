using System.Linq;
using UnityEngine;

namespace SahurRaising
{
    public static class StringUtils
    {
        /// <summary>
        /// 문자열에서 문자와 숫자를 한 번에 추출합니다.
        /// </summary>
        public static (string letters, int number) ParseLettersAndNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return (string.Empty, 0);

            int length = input.Length;
            int letterCount = 0;
            int number = 0;

            // 첫 번째 패스: 문자 개수와 숫자 추출
            for (int i = 0; i < length; i++)
            {
                char c = input[i];
                if (char.IsLetter(c))
                    letterCount++;
                else if (char.IsDigit(c))
                    number = number * 10 + (c - '0');
            }

            // 두 번째 패스: 문자 추출
            if (letterCount == 0)
                return (string.Empty, number);

            // 정확한 크기로 한 번만 할당
            char[] buffer = new char[letterCount];
            int index = 0;

            for (int i = 0; i < length; i++)
            {
                if (char.IsLetter(input[i]))
                    buffer[index++] = input[i];
            }

            return (new string(buffer), number);
        }

        /// <summary>
        /// 문자열에서 문자 부분만 추출합니다.
        /// </summary>
        public static string ExtractLetters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            int length = input.Length;
            int letterCount = 0;

            // 먼저 문자 개수 카운트
            for (int i = 0; i < length; i++)
            {
                if (char.IsLetter(input[i]))
                    letterCount++;
            }

            if (letterCount == 0)
                return string.Empty;

            // 정확한 크기로 한 번만 할당
            char[] buffer = new char[letterCount];
            int index = 0;

            for (int i = 0; i < length; i++)
            {
                if (char.IsLetter(input[i]))
                    buffer[index++] = input[i];
            }

            return new string(buffer);
        }

        /// <summary>
        /// 문자열에서 숫자 부분만 추출합니다.
        /// </summary>
        public static int ExtractNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            int number = 0;
            int length = input.Length;

            for (int i = 0; i < length; i++)
            {
                char c = input[i];
                if (char.IsDigit(c))
                {
                    number = number * 10 + (c - '0');
                }
            }

            return number;
        }

        /// <summary>
        /// 업그레이드 코드(예: UP101)에서 티어 인덱스를 추출합니다.
        /// UP001 -> 0, UP101 -> 1, UP201 -> 2
        /// </summary>
        public static int ExtractTierFromCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 3)
                return 0;

            // "UP" 제거 시도, 만약 UP으로 시작하지 않으면 전체에서 숫자 파싱
            string numberPart = code.StartsWith("UP") ? code.Substring(2) : code;

            if (int.TryParse(numberPart, out int number))
            {
                // 100단위로 티어 구분
                return number / 100;
            }

            return 0;
        }
    }
}
