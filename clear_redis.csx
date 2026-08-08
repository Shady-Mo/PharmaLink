#r "nuget: StackExchange.Redis"

using System;
using System.Threading.Tasks;
using StackExchange.Redis;

var redis = await ConnectionMultiplexer.ConnectAsync("retrospeedy-macrofresh-titanic-72406.db.redis.io:16881,password=3DTABahwRhxIKj4G0lDeppDqi3FUzHKD");
var db = redis.GetDatabase();
await db.KeyDeleteAsync("cart:3954a50f-0847-4388-4407-08def08e9f03");
Console.WriteLine("Redis key deleted.");
