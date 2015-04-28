# SimplerProtobufParser
Simpler ProtobufParser use pure C# for Unity and all .net project

纯C#代码实现了协议字节流的解析和生成。现主要给u3d编写，其他.net环境同样适用。
本代码不用使用proto文件，全手动用C#构建和解析协议，编码量较大，无自动生成协议类，无法简化编码工作。
当对于一些要求苛刻的平台（如ios），不会遇到任何使用限制。
效率没有优化，不要在效率吃紧的环境使用。


proto file like it:

----------------------------------------------------------Start

message SubMessage

{

	required float x = 1;
	
	required float y = 2;
	
	required float z = 3;
	
}

message MainMessage

{

	required int32 id = 1;
	
	required Vector3 pos = 2;
	
	repeated string names = 3;
}

----------------------------------------------------------End


C# code:

----------------------------------------------------------Start

print("Build Data:");

CxProtobufSender senderMsg = new CxProtobufSender();

//1:id(int)

senderMsg.WriteInt32(1,-1234567890);

//2:pos(SubMessage)

CxProtobufSender vectorMsg = new CxProtobufSender();

vectorMsg.WriteFloat(1, 1);

vectorMsg.WriteFloat(2, 2);

vectorMsg.WriteFloat(3, 3);

senderMsg.WriteSubMessage(2,vectorMsg);

//3:names(repeated)

senderMsg.WriteString(3, "haha");

senderMsg.WriteString(3, "nihao");

senderMsg.WriteString(3, "niubi");



byte[] b = senderMsg.GetData();

CxProtobufData pbData = CxProtobufParser.instance.Parser(b);

print("Get Data:");

print("1:" + pbData.GetInt32(1));

CxProtobufData childData = pbData.GetSubMessage(2);



print("2:subMessage");

print(string.Format("vector:{0},{1},{2}", childData.GetFloat(1), childData.GetFloat(2), childData.GetFloat(3)));



var arr = pbData.GetRepeated(3);

print("3:repeated");

foreach(var a in arr)

{

    print("->" + a.GetString(1));
    
}

----------------------------------------------------------End

