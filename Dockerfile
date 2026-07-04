FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/FCG.Payments.Worker/FCG.Payments.Worker.csproj", "src/FCG.Payments.Worker/"]
COPY ["src/FCG.Payments.Application/FCG.Payments.Application.csproj", "src/FCG.Payments.Application/"]

RUN dotnet restore "src/FCG.Payments.Worker/FCG.Payments.Worker.csproj"

COPY . .

WORKDIR "/src/src/FCG.Payments.Worker"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FCG.Payments.Worker.dll"]
