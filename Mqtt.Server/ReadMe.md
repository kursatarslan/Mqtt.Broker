#deploy
docker build -t mqttbroker .
docker tag mqttbroker kursatarslan/mqttbroker
docker build -t kursatarslan/mqttbroker .
 docker push kursatarslan/mqttbroker  


# Local
docker run -p 5432:5432 --name postgres -e POSTGRES_PASSWORD=postgres -d postgres


# you can generate db using this command
dotnet ef database update


Mqtt.Context git:(master) ✗ dotnet ef migrations add InitialCreate --startup-project ../Mqtt.Server/Mqtt.Server.csproj 
#



