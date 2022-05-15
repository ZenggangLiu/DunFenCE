/**
 *  Define the Common Encoding Related Algorithms
 *    string sString = "\u0000ab";
 *    //sString = "\x0000abc";
 *    uint[] key = { 0x61626364, 0, 0, 0 };// 494743, 671417, 800647, 999907 };
 *    uint[] values = { 0x00616200, 0, 0, 0 }; //1, 2, 3, 4, 5, 6 };
 *    Framework.Utils.Encoding.encryptXXTEA(values, key);
 *    // Framework.Utils.Encoding.decryptXXTEA(values, key);
 *    string sKey = "abcd";
 *    Framework.Utils.Encoding.encryptXXTEA(sString, sKey);
 *    System.String json_string = "{\n  \"snsConfig\": {\n    \"Win32\": {\n      \"Test\": 1,\n      \"Ldap\": 1\n    },\n    \"iPhone\": {\n      \"GameCenter\": {\n        \"appID\": \"900133047\"\n      },\n      \"Facebook\": {\n        \"appID\": \"1445497355712845\",\n        \"gameExtension\": \"pp\"\n      }\n    },\n    \"Windows8\": {\n      \"Facebook\": {\n        \"appID\": \"1445497355712845\",\n        \"gameExtension\": \"pp\"\n      }\n    },\n    \"WindowsPhone8\": {\n      \"Facebook\": {\n        \"appID\": \"1445497355712845\",\n        \"gameExtension\": \"pp\"\n      }\n    },\n    \"Android\": {\n      \"GameAPI\": 1,\n      \"Facebook\": {\n        \"appID\": \"1445497355712845\",\n        \"gameExtension\": \"pp\"\n      }\n    }\n  }\n}";
 */
namespace Framework.Utils
{
    public static class Encoding
    {
        /**
         *  push string left to right --> highest to lowest byte of a_iIntValue: 0 as padding character
         */
        public static void asInt(string a_sString, 
                                 int a_uStartingIndex, 
                                 ref uint a_iIntValue)
        {
            a_iIntValue = 0;
            for(byte _iIdx = 0; _iIdx < 4; ++_iIdx)
            {
                if(a_uStartingIndex >= a_sString.Length)
                {
                    return;
                }

                byte _iChar = (byte)(a_sString [a_uStartingIndex++] & 0xFF); // just take the lower 8bits
                a_iIntValue |= (uint)(_iChar << (3 - _iIdx) * 8);
            }
        }

        /**
         *  Encrypt the given integer array with XXTEA algorithm
         *  a_sKeyMax16Chars: first 16 characters count
         */
        public static void encryptXXTEA(string a_sClearText, /** no way to provide c++ type const on arguments */
                                        string a_sKeyMax16Chars)
        {
            // copy the keys: maximal 16 characters: each uint stores 4 characters
            uint[] _aKey = new uint[4];
            for(byte _iIdx = 0; _iIdx < (byte)_aKey.Length; ++_iIdx)
            {
                // store each 4 characters into an uint
                asInt(a_sKeyMax16Chars, _iIdx * 4, ref _aKey [_iIdx]);
            }

            // get the count of padding chars
            byte _iPaddingCharCount = (byte)((uint)a_sClearText.Length & 0x3);

            // get the array element count
            uint _uArrayLenght = (uint)(a_sClearText.Length + (0 == _iPaddingCharCount ? 0 : 1));

            // create the uint array TODO: get from argument
            uint[] _aCharArray = new uint[_uArrayLenght];

            // copy the given clear text
            for(byte _iIdx = 0; _iIdx < (byte)_aCharArray.Length; ++_iIdx)
            {
                // store each 4 characters into an uint
                asInt(a_sClearText, _iIdx * 4, ref _aCharArray [_iIdx]);
            }

            // encrypt
            encryptXXTEA(_aCharArray, _aKey);
        }

        /**
         *  Encrypt the given integer array with XXTEA algorithm
         *  a_aClearTextBuffer must be an array of at least 4 elements
         *  a_aKeys must be an array of 4 elements
         */
        public static void encryptXXTEA(uint[] a_aClearTextBuffer, /** no way to provide c++ type const on arguments */
                                        uint[] a_aKeys)
        {
            uint _uY;
            uint _uZ;
            uint _uSum;
            uint _uP;
            uint _uRounds;
            uint _uE;

            uint _uDataWordLength = (uint)a_aClearTextBuffer.Length;
            if(0 == _uDataWordLength)
            {
                return;
            }

            if(a_aKeys.Length != 4)
            {
                return;
            }

            // Coding Part
            _uRounds = 6 + 52 / _uDataWordLength;
            _uSum = 0;
            _uZ = a_aClearTextBuffer [_uDataWordLength - 1];

            do
            {
                _uSum += 0x9e3779b9;
                _uE = (_uSum >> 2) & 3;

                for(_uP = 0; _uP < _uDataWordLength - 1; _uP++)
                {
                    _uY = a_aClearTextBuffer [_uP + 1];
                    //uint xxx = (uint)(long)((_uY >> 3 ^ _uZ << 4) ^ ((_uSum ^ _uY) + (a_aKeys [(_uP & 3) ^ _uE] ^ _uZ)));
                    //xxx = a_aClearTextBuffer[_uP];
                    _uZ = a_aClearTextBuffer [_uP] += (((_uZ >> 5 ^ _uY << 2) + (_uY >> 3 ^ _uZ << 4)) ^ ((_uSum ^ _uY) + (a_aKeys [(_uP & 3) ^ _uE] ^ _uZ)));
                }

                _uY = a_aClearTextBuffer [0];
                _uZ = a_aClearTextBuffer [_uDataWordLength - 1] += (((_uZ >> 5 ^ _uY << 2) + (_uY >> 3 ^ _uZ << 4)) ^ ((_uSum ^ _uY) + (a_aKeys [(_uP & 3) ^ _uE] ^ _uZ)));
            } while(0 != --_uRounds);
        }

        /**
         *  Decrypt the given integer array with XXTEA algorithm
         *  a_aEncryptedTextBuffer must be an array of at least 4 elements
         *  a_aKeys must be an array of 4 elements
         */
        public static void decryptXXTEA(uint[] a_aEncryptedTextBuffer, /** no way to provide c++ type const on arguments */
                                        uint[] a_aKeys)
        {
            uint _uY;
            uint _uZ;
            uint _uSum;
            uint _uP;
            uint _uRounds;
            uint _uE;

            uint _uDataWordLength = (uint)a_aEncryptedTextBuffer.Length;
            if(0 == _uDataWordLength)
            {
                return;
            }

            if(a_aKeys.Length != 4)
            {
                return;
            }

            // Decoding Part
            _uRounds = 6 + 52 / _uDataWordLength;
            _uSum = _uRounds * 0x9e3779b9;
            _uY = a_aEncryptedTextBuffer [0];

            do
            {
                _uE = (_uSum >> 2) & 3;

                for(_uP = _uDataWordLength - 1; _uP > 0; _uP--)
                {
                    _uZ = a_aEncryptedTextBuffer [_uP - 1];
                    _uY = a_aEncryptedTextBuffer [_uP] -= (((_uZ >> 5 ^ _uY << 2) + (_uY >> 3 ^ _uZ << 4)) ^ ((_uSum ^ _uY) + (a_aKeys [(_uP & 3) ^ _uE] ^ _uZ)));
                }

                _uZ = a_aEncryptedTextBuffer [_uDataWordLength - 1];
                _uY = a_aEncryptedTextBuffer [0] -= (((_uZ >> 5 ^ _uY << 2) + (_uY >> 3 ^ _uZ << 4)) ^ ((_uSum ^ _uY) + (a_aKeys [(_uP & 3) ^ _uE] ^ _uZ)));
                _uSum -= 0x9e3779b9;
            } while(0 != --_uRounds);
        }
    };
} // namespace Framework.Utils
