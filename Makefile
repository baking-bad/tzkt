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

paris-init:
	docker-compose -f docker-compose.paris.yml up   -d paris-db
	docker-compose -f docker-compose.paris.yml exec -T paris-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.paris.yml exec -T paris-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.paris.yml exec -T paris-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.paris.yml exec -T paris-db apt update
	docker-compose -f docker-compose.paris.yml exec -T paris-db apt install -y wget
	docker-compose -f docker-compose.paris.yml exec -T paris-db wget "https://snapshots.tzkt.io/tzkt_v1.14_parisnet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.paris.yml exec -T paris-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.paris.yml exec -T paris-db rm tzkt_db.backup
	docker-compose -f docker-compose.paris.yml exec -T paris-db apt autoremove --purge -y wget
	docker-compose pull	
	
paris-start:
	docker-compose -f docker-compose.paris.yml up -d

paris-stop:
	docker-compose -f docker-compose.paris.yml down

paris-db-start:
	docker-compose -f docker-compose.paris.yml up -d paris-db
reset:
	docker-compose -f docker-compose.paris.yml down --volumes
	docker-compose -f docker-compose.paris.yml up -d paris-db