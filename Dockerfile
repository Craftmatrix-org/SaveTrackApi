FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

RUN echo "Starting to Build the API..."

WORKDIR /app

COPY . ./
# COPY .env.deployment .env

RUN dotnet nuget locals all --clear
RUN dotnet restore

RUN dotnet build -c Release

# RUN dotnet tool install --global dotnet-ef
# ENV PATH="$PATH:/root/.dotnet/tools"
# RUN dotnet ef database update


RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build-env /app/out .

# Exposing the Port 80
EXPOSE 80

# Starting the APO
ENTRYPOINT ["dotnet", "SaveTrackApi.dll"]
