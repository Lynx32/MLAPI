# CryptographyHelper.Encrypt Method 
 

Encrypts a message with AES with a given key and a random salt that gets encoded as the first 16 bytes of the encrypted buffer

**Namespace:**&nbsp;<a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents</a><br />**Assembly:**&nbsp;MLAPI (in MLAPI.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public static byte[] Encrypt(
	byte[] clearBuffer,
	byte[] key
)
```

<br />

#### Parameters
&nbsp;<dl><dt>clearBuffer</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">System.Byte</a>[]<br />The buffer to be encrypted</dd><dt>key</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">System.Byte</a>[]<br />The key to use</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/yyb1w04y" target="_blank">Byte</a>[]<br />The encrypted byte array with encoded salt

## See Also


#### Reference
<a href="T_MLAPI_NetworkingManagerComponents_CryptographyHelper">CryptographyHelper Class</a><br /><a href="N_MLAPI_NetworkingManagerComponents">MLAPI.NetworkingManagerComponents Namespace</a><br />