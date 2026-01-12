init:
	docker-compose up   -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db apt update
	docker-compose exec -T db apt install -y wget
	docker-compose exec -T db wget "https://snapshots.tzkt.io/tzkt_v1.17_mainnet.backup" -O tzkt_db.backup
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

shadownet-init:
	docker-compose -f docker-compose.shadownet.yml up   -d shadownet-db
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db apt update
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db apt install -y wget
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db wget "https://snapshots.tzkt.io/tzkt_v1.17_shadownet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db rm tzkt_db.backup
	docker-compose -f docker-compose.shadownet.yml exec -T shadownet-db apt autoremove --purge -y wget
	docker-compose pull	
	
shadownet-start:
	docker-compose -f docker-compose.shadownet.yml up -d

shadownet-stop:
	docker-compose -f docker-compose.shadownet.yml down

shadownet-db-start:
	docker-compose -f docker-compose.shadownet.yml up -d shadownet-db

tallinnnet-init:
	docker-compose -f docker-compose.tallinnnet.yml up   -d tallinnnet-db
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db apt update
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db apt install -y wget
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db wget "https://snapshots.tzkt.io/tzkt_v1.17_tallinnnet.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db rm tzkt_db.backup
	docker-compose -f docker-compose.tallinnnet.yml exec -T tallinnnet-db apt autoremove --purge -y wget
	docker-compose pull	
	
tallinnnet-start:
	docker-compose -f docker-compose.tallinnnet.yml up -d

tallinnnet-stop:
	docker-compose -f docker-compose.tallinnnet.yml down

tallinnnet-db-start:
	docker-compose -f docker-compose.tallinnnet.yml up -d tallinnnet-db