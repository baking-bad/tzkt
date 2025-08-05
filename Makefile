init:
	docker-compose up   -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db apt update
	docker-compose exec -T db apt install -y wget
	docker-compose exec -T db wget "https://snapshots.tzkt.io/tzkt_v1.16_mainnet.backup" -O tzkt_db.backup
	docker-compose exec -T db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose exec -T db rm tzkt_db.backup
	docker-compose exec -T db apt autoremove --purge -y wget
	docker-compose pull	

start:
	docker-compose up -d

stop:
	docker-compose down

update:
	git pull
	docker-compose build

clean:
	docker system prune --force

db-start:
	docker-compose up -d db

migration:
	# Install EF: dotnet tool install --global dotnet-ef
	export $$(cat .env | xargs) && cd Tzkt.Data && dotnet-ef database update -s ../Tzkt.Sync/Tzkt.Sync.csproj

sync:
	# Set up env file: cp .env.sample .env
	export $$(cat .env | xargs) && dotnet run -p Tzkt.Sync -v normal

api:
	# Set up env file: cp .env.sample .env
	export $$(cat .env | xargs) && dotnet run -p Tzkt.Api -v normal

api-image:
	docker build -t bakingbad/tzkt-api:latest -f ./Tzkt.Api/Dockerfile .

sync-image:
	docker build -t bakingbad/tzkt-sync:latest -f ./Tzkt.Sync/Dockerfile .

ghostnet-init:
	docker-compose -f docker-compose.ghostnet.yml up   -d ghostnet-db
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db apt update
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db apt install -y wget
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db wget "https://snapshots.tzkt.io/tzkt_v1.16_ghostnet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db rm tzkt_db.backup
	docker-compose -f docker-compose.ghostnet.yml exec -T ghostnet-db apt autoremove --purge -y wget
	docker-compose pull	
	
ghostnet-start:
	docker-compose -f docker-compose.ghostnet.yml up -d

ghostnet-stop:
	docker-compose -f docker-compose.ghostnet.yml down

ghostnet-db-start:
	docker-compose -f docker-compose.ghostnet.yml up -d ghostnet-db

seoulnet-init:
	docker-compose -f docker-compose.seoulnet.yml up   -d seoulnet-db
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db apt update
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db apt install -y wget
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db wget "https://snapshots.tzkt.io/tzkt_v1.16_seoulnet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db rm tzkt_db.backup
	docker-compose -f docker-compose.seoulnet.yml exec -T seoulnet-db apt autoremove --purge -y wget
	docker-compose pull	
	
seoulnet-start:
	docker-compose -f docker-compose.seoulnet.yml up -d

seoulnet-stop:
	docker-compose -f docker-compose.seoulnet.yml down

seoulnet-db-start:
	docker-compose -f docker-compose.seoulnet.yml up -d seoulnet-db