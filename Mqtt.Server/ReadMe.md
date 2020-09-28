#deploy
docker build -t mqttbroker .
docker tag mqttbroker kursatarslan/mqttbroker
docker build -t kursatarslan/mqttbroker .
 docker push kursatarslan/mqttbroker  
 
#Create a self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365
openssl pkcs12 -export -out webserver.pfx -inkey key.pem -in cert.pem
#adding dockerfile
# ENV ASPNETCORE_Kestrel__Certificates__Default__Path="./webserver.pfx"
# ENV ASPNETCORE_Kestrel__Certificates__Default__Password="{yourExportPassword}" \

# Local
docker run -p 5432:5432 --name postgres -e POSTGRES_PASSWORD=postgres -d postgres

# you can generate db using this command
dotnet ef database update

# migration
dotnet ef migrations add InitialCreate --startup-project ../Mqtt.Server/Mqtt.Server.csproj 




