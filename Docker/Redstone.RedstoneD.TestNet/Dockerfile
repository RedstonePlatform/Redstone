FROM microsoft/dotnet:2.1-sdk

RUN git clone https://github.com/RedstonePlatform/Redstone.git \
    && cd /Redstone/src/Redstone/Programs/Redstone.RedstoneFullNodeD \
	&& dotnet build

VOLUME /root/.stratisnode/redstone/RedstoneTest

WORKDIR /Redstone/src/Redstone/Programs/Redstone.RedstoneFullNodeD

EXPOSE 19056 19057 37222 19156 19157 38222

CMD ["dotnet", "run", "-testnet"]