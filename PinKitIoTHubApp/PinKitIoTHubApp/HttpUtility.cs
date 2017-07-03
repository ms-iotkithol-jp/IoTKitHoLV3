//  --------------------------------------------------------------------------------- 
//  Copyright (c) Microsoft Corporation  All rights reserved. 
//  
// Microsoft Public License (Ms-PL)
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// 
// 1. Definitions
// 
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// 
// A "contributor" is any person that distributes its contribution under this license.
// 
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// 
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// 
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// 
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// 
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// 
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// 
// Written by Hiroshi Ota 
// Twitter http://www.twitter.com/embedded_george
// Blog    http://blogs.msdn.com/hirosho
//  --------------------------------------------------------------------------------- 

using System;
using Microsoft.SPOT;
using System.Text;

namespace EGIoTKit.Utility
{
    /// <summary>
    /// Utility for Http URL Encoding
    /// </summary>
    public static class HttpUtility
    {
        /// <summary>
        /// Encode string by Url encoding
        /// </summary>
        /// <param name="s">source string to be encode</param>
        /// <returns>Url encoded string</returns>
        public static string UrlEncode(string s)
        {
            char[] encodedChar = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            int num0 = '0';
            int num9 = '9';
            int chara = 'a';
            int charz = 'z';
            int charA = 'A';
            int charZ = 'Z';
            string encoded = "";
            foreach (var c in s.ToCharArray())
            {
                int cv = c;
                if ((num0 <= cv && cv <= num9) || (chara <= cv && cv <= charz) || (charA <= cv && cv <= charZ))
                {
                    encoded += c;
                }
                else
                {
                    if (cv != ' ' && cv != '-' && cv != '.')
                    {
                        encoded += "%";
                        int ch = c >> 4;
                        int cl = c & 0x0f;
                        if (ch > 0x8)
                        {
                            throw new ArgumentOutOfRangeException("should be ascii code");
                        }
                        encoded += encodedChar[ch];
                        encoded += encodedChar[cl];
                    }
                    else
                    {
                        if (cv == ' ')
                        {
                            encoded += "+";
                        }
                        else if (cv == '-')
                        {
                            encoded += "-";
                        }
                        else if (cv == '.')
                        {
                            encoded += ".";
                        }
                    }
                }
            }
            return encoded;
        }

        /// <summary>
        /// Decode string by Url encoded
        /// </summary>
        /// <param name="s">source string which is to be decoded</param>
        /// <returns>Decoded string</returns>
        public static string UrlDecode(string s)
        {
            if (s == null) return null;
            if (s.Length < 1) return s;

            char[] chars = s.ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            int count = chars.Length;
            int dstIndex = 0;
            int srcIndex = 0;

            while (true)
            {
                if (srcIndex >= count)
                {
                    if (dstIndex < srcIndex)
                    {
                        byte[] sizedBytes = new byte[dstIndex];
                        Array.Copy(bytes, 0, sizedBytes, 0, dstIndex);
                        bytes = sizedBytes;
                    }
                    return new string(Encoding.UTF8.GetChars(bytes));
                }

                if (chars[srcIndex] == '+')
                {
                    bytes[dstIndex++] = (byte)' ';
                    srcIndex += 1;
                }
                else if (chars[srcIndex] == '%' && srcIndex < count - 2)
                    if (chars[srcIndex + 1] == 'u' && srcIndex < count - 5)
                    {
                        int ch1 = HexToInt(chars[srcIndex + 2]);
                        int ch2 = HexToInt(chars[srcIndex + 3]);
                        int ch3 = HexToInt(chars[srcIndex + 4]);
                        int ch4 = HexToInt(chars[srcIndex + 5]);

                        if (ch1 >= 0 && ch2 >= 0 && ch3 >= 0 && ch4 >= 0)
                        {
                            bytes[dstIndex++] = (byte)((ch1 << 4) | ch2);
                            bytes[dstIndex++] = (byte)((ch3 << 4) | ch4);
                            srcIndex += 6;
                            continue;
                        }
                    }
                    else
                    {
                        int ch1 = HexToInt(chars[srcIndex + 1]);
                        int ch2 = HexToInt(chars[srcIndex + 2]);

                        if (ch1 >= 0 && ch2 >= 0)
                        {
                            bytes[dstIndex++] = (byte)((ch1 << 4) | ch2);
                            srcIndex += 3;
                            continue;
                        }
                    }
                else
                {
                    byte[] charBytes = Encoding.UTF8.GetBytes(chars[srcIndex++].ToString());
                    charBytes.CopyTo(bytes, dstIndex);
                    dstIndex += charBytes.Length;
                }
            }
        }

        private static int HexToInt(char ch)
        {
            return
                (ch >= '0' && ch <= '9') ? ch - '0' :
                (ch >= 'a' && ch <= 'f') ? ch - 'a' + 10 :
                (ch >= 'A' && ch <= 'F') ? ch - 'A' + 10 :
                -1;
        }
    }
}
