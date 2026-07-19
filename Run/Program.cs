// See https://aka.ms/new-console-template for more information

using ChatBot.Commons;
using Microsoft.Extensions.Options;

//Console.WriteLine((EncryptionService.GenerateKey()));

string key = EncryptionService.GenerateKey();


IOptions<EncryptionOptions> options = Options.Create(new EncryptionOptions { Key = key });
var encryptService = new EncryptionService(options);

var a = encryptService.Encrypt("Host=localhost;Port=5432;Database=SL_ChatBot_RAG;Username=Admin;Password=Admin123456789");
var b = encryptService.Decrypt(a);
Console.WriteLine(key);
Console.WriteLine(a);
Console.WriteLine(b);
Console.ReadLine(); 
