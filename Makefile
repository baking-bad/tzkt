init:
	docker-compose up   -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db apt update
	docker-compose exec -T db apt install -y wget
	docker-compose exec -T db wget "https://snapshots.tzkt.io/tzkt_v1.12_mainnet.backup" -O tzkt_db.backup
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
	docker-compose -f docker-compose.ghost.yml exec -T ghost-db wget "https://snapshots.tzkt.io/tzkt_v1.12_ghostnet.backup" -O tzkt_db.backup
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

lima-init:
	docker-compose -f docker-compose.lima.yml up   -d lima-db
	docker-compose -f docker-compose.lima.yml exec -T lima-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.lima.yml exec -T lima-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.lima.yml exec -T lima-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.lima.yml exec -T lima-db apt update
	docker-compose -f docker-compose.lima.yml exec -T lima-db apt install -y wget
	docker-compose -f docker-compose.lima.yml exec -T lima-db wget "https://snapshots.tzkt.io/tzkt_v1.11_limanet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.lima.yml exec -T lima-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.lima.yml exec -T lima-db rm tzkt_db.backup
	docker-compose -f docker-compose.lima.yml exec -T lima-db apt autoremove --purge -y wget
	docker-compose pull	
	
lima-start:
	docker-compose -f docker-compose.lima.yml up -d

lima-stop:
	docker-compose -f docker-compose.lima.yml down

lima-db-start:
	docker-compose -f docker-compose.lima.yml up -d lima-db

mumbai-init:
	docker-compose -f docker-compose.mumbai.yml up   -d mumbai-db
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db apt update
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db apt install -y wget
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db wget "https://snapshots.tzkt.io/tzkt_v1.12_mumbainet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db rm tzkt_db.backup
	docker-compose -f docker-compose.mumbai.yml exec -T mumbai-db apt autoremove --purge -y wget
	docker-compose pull	
	
mumbai-start:
	docker-compose -f docker-compose.mumbai.yml up -d

mumbai-stop:
	docker-compose -f docker-compose.mumbai.yml down

mumbai-db-start:
	docker-compose -f docker-compose.mumbai.yml up -d mumbai-db