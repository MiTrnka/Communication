using Grpc.Core;
using GrpcServer;

namespace gRPCServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        /*private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }*/

        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            string odpoved;
            if ((request == null) || (request.Name == null) || (request.Age == null)) 
                odpoved = "Odpov�� ze serveru: Na klientovi jste zavolal serverovou metodu SayHello obsahuj�c� n�kde null";
            else
                odpoved = "Odpov�� ze serveru: Na klientovi jste zavolal serverovou metodu SayHello s parametry: " + request.Name+" "+request.Age.ToString();
            
            return new HelloReply { Message = odpoved };

        }
    }
}
