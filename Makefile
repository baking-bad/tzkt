init:
	docker-compose up   -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db apt update
	docker-compose exec -T db apt install -y wget
	docker-compose exec -T db wget "https://snapshots.tzkt.io/tzkt_v1.14_mainnet.backup" -O tzkt_db.backup
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

ghost-init:
	docker-compose -f docker-compose.ghost.yml up   -d ghost-db
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db apt update
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db apt install -y wget
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db wget "https://snapshots.tzkt.io/tzkt_v1.14_ghostnet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db rm tzkt_db.backup
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db apt autoremove --purge -y wget
	docker-compose pull	
	
ghost-start:
	docker-compose -f docker-compose.ghost.yml up -d

ghost-stop:
	docker-compose -f docker-compose.ghost.yml down

ghost-db-start:
	docker-compose -f docker-compose.ghost.yml up -d ghost-db

rio-init:
	docker-compose -f docker-compose.rio.yml up   -d rio-db
	docker-compose -f docker-compose.rio.yml exec -T rio-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.rio.yml exec -T rio-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.rio.yml exec -T rio-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.rio.yml exec -T rio-db apt update
	docker-compose -f docker-compose.rio.yml exec -T rio-db apt install -y wget
	docker-compose -f docker-compose.rio.yml exec -T rio-db wget "https://snapshots.tzkt.io/tzkt_v1.14_rionet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.rio.yml exec -T rio-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.rio.yml exec -T rio-db rm tzkt_db.backup
	docker-compose -f docker-compose.rio.yml exec -T rio-db apt autoremove --purge -y wget
	docker-compose pull	
	
rio-start:
	docker-compose -f docker-compose.rio.yml up -d

rio-stop:
	docker-compose -f docker-compose.rio.yml down

rio-db-start:
	docker-compose -f docker-compose.rio.yml up -d rio-db
reset:
	docker-compose -f docker-compose.rio.yml down --volumes
	docker-compose -f docker-compose.rio.yml up -d rio-db