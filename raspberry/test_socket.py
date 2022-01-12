import socket
import sys

HOST = '192.168.0.3'
PORT = 8888

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
try:
    client.connect((HOST, PORT))
except Exception as e:
    print("Socket Connection Fail")
    sys.exit()

print("Connected")



msg=input()
client.send(msg.encode(encoding='utf_8', errors='strict'))
data = client.recv(1024)
print ('result: ' + data.decode())

client.close()
