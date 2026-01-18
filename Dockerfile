FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Payment.API/Payment.API.csproj", "Payment.API/"]
COPY ["Payment.Processor/Payment.Processor.csproj", "Payment.Processor/"]
COPY ["Payment.Domain/Payment.Domain.csproj", "Payment.Domain/"]

RUN dotnet restore "Payment.API/Payment.API.csproj"
RUN dotnet restore "Payment.Processor/Payment.Processor.csproj"

COPY . .

RUN dotnet publish "Payment.API/Payment.API.csproj" -c Release -o /app/publish_api
RUN dotnet publish "Payment.Processor/Payment.Processor.csproj" -c Release -o /app/publish_processor

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api-final
WORKDIR /app
COPY --from=build /app/publish_api .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Payment.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS processor-final
WORKDIR /app
COPY --from=build /app/publish_processor .
ENTRYPOINT ["dotnet", "Payment.Processor.dll"]