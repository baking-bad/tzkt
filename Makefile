init:
	docker-compose up   -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db apt update
	docker-compose exec -T db apt install -y wget
	docker-compose exec -T db wget "https://snapshots.tzkt.io/tzkt_v1.13_mainnet.backup" -O tzkt_db.backup
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
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db wget "https://snapshots.tzkt.io/tzkt_v1.13_ghostnet.backup" -O tzkt_db.backup
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

nairobi-init:
	docker-compose -f docker-compose.nairobi.yml up   -d nairobi-db
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db apt update
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db apt install -y wget
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db wget "https://snapshots.tzkt.io/tzkt_v1.12_nairobinet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db rm tzkt_db.backup
	docker-compose -f docker-compose.nairobi.yml exec -T nairobi-db apt autoremove --purge -y wget
	docker-compose pull	
	
nairobi-start:
	docker-compose -f docker-compose.nairobi.yml up -d

nairobi-stop:
	docker-compose -f docker-compose.nairobi.yml down

nairobi-db-start:
	docker-compose -f docker-compose.nairobi.yml up -d nairobi-db

oxford-init:
	docker-compose -f docker-compose.oxford.yml up   -d oxford-db
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db apt update
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db apt install -y wget
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db wget "https://snapshots.tzkt.io/tzkt_v1.13_oxfordnet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db rm tzkt_db.backup
	docker-compose -f docker-compose.oxford.yml exec -T oxford-db apt autoremove --purge -y wget
	docker-compose pull	
	
oxford-start:
	docker-compose -f docker-compose.oxford.yml up -d

oxford-stop:
	docker-compose -f docker-compose.oxford.yml down

oxford-db-start:
	docker-compose -f docker-compose.oxford.yml up -d oxford-db
reset:
	docker-compose -f docker-compose.oxford.yml down --volumes
	docker-compose -f docker-compose.oxford.yml up -d oxford-db