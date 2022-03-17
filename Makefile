init:
	docker-compose up   -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db apt update
	docker-compose exec -T db apt install -y wget
	docker-compose exec -T db wget "https://tzkt.fra1.digitaloceanspaces.com/snapshots/tzkt_v1.8_mainnet.backup" -O tzkt_db.backup
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
	
hangzhou-init:
	docker-compose -f docker-compose.hangzhou.yml up   -d hangzhou-db
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db apt update
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db apt install -y wget
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db wget "https://tzkt.fra1.digitaloceanspaces.com/snapshots/tzkt_v1.8_hangzhou2net.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db rm tzkt_db.backup
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db apt autoremove --purge -y wget
	docker-compose pull	
	
hangzhou-start:
	docker-compose -f docker-compose.hangzhou.yml up -d

hangzhou-stop:
	docker-compose -f docker-compose.hangzhou.yml down

hangzhou-db-start:
	docker-compose -f docker-compose.hangzhou.yml up -d hangzhou-db
	
ithaca-init:
	docker-compose -f docker-compose.ithaca.yml up   -d ithaca-db
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db apt update
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db apt install -y wget
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db wget "https://tzkt.fra1.digitaloceanspaces.com/snapshots/tzkt_v1.8_ithaca.backup" -O tzkt_db.backup
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 4 tzkt_db.backup
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db rm tzkt_db.backup
	docker-compose -f docker-compose.ithaca.yml exec -T ithaca-db apt autoremove --purge -y wget
	docker-compose pull	
	
ithaca-start:
	docker-compose -f docker-compose.ithaca.yml up -d

ithaca-stop:
	docker-compose -f docker-compose.ithaca.yml down

ithaca-db-start:
	docker-compose -f docker-compose.ithaca.yml up -d ithaca-db