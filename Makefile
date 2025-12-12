init:
	docker compose up   -d db
	docker compose exec -T db psql -U mvkt postgres -c '\l'
	docker compose exec -T db dropdb -U mvkt --if-exists mvkt_db
	docker compose exec -T db createdb -U mvkt -T template0 mvkt_db
	docker compose exec -T db apt update
	docker compose exec -T db apt install -y wget
	docker compose exec -T db wget "https://snapshots.mvkt.io/tzkt_v1.14_mainnet.backup" -O mvkt_db.backup
	docker compose exec -T db pg_restore -U mvkt -O -x -v -d mvkt_db -e -j 4 mvkt_db.backup
	docker compose exec -T db rm mvkt_db.backup
	docker compose exec -T db apt autoremove --purge -y wget
	docker compose pull	

start:
	docker compose up -d

stop:
	docker compose down

update:
	git pull
	docker compose build

clean:
	docker system prune --force

db-start:
	docker compose up -d db

migration:
	# Install EF: dotnet tool install --global dotnet-ef
	export $$(cat .env | xargs) && cd Mvkt.Data && dotnet-ef database update -s ../Mvkt.Sync/Mvkt.Sync.csproj

sync:
	# Set up env file: cp .env.sample .env
	export $$(cat .env | xargs) && dotnet run -p Mvkt.Sync -v normal

api:
	# Set up env file: cp .env.sample .env
	export $$(cat .env | xargs) && dotnet run -p Mvkt.Api -v normal

test:
	dotnet test --verbosity normal

test-api:
	dotnet test Mvkt.Api.Tests --verbosity normal

api-image:
	docker build -t mavrykdynamics/mvkt-api:latest -f ./Mvkt.Api/Dockerfile .

sync-image:
	docker build -t mavrykdynamics/mvkt-sync:latest -f ./Mvkt.Sync/Dockerfile .

base-init:
	docker compose -f docker compose.base.yml up   -d base-db
	docker compose -f docker compose.base.yml exec -T base-db psql -U mvkt postgres -c '\l'
	docker compose -f docker compose.base.yml exec -T base-db dropdb -U mvkt --if-exists mvkt_db
	docker compose -f docker compose.base.yml exec -T base-db createdb -U mvkt -T template0 mvkt_db
	docker compose -f docker compose.base.yml exec -T base-db apt update
	docker compose -f docker compose.base.yml exec -T base-db apt install -y wget
	docker compose -f docker compose.base.yml exec -T base-db wget "https://snapshots.mvkt.io/tzkt_v1.14_ghostnet.backup" -O mvkt_db.backup
	docker compose -f docker compose.base.yml exec -T base-db pg_restore -U mvkt -O -x -v -d mvkt_db -e -j 4 mvkt_db.backup
	docker compose -f docker compose.base.yml exec -T base-db rm mvkt_db.backup
	docker compose -f docker compose.base.yml exec -T base-db apt autoremove --purge -y wget
	docker compose pull	
	
base-start:
	docker compose -f docker compose.base.yml up -d

base-stop:
	docker compose -f docker compose.base.yml down

base-db-start:
	docker compose -f docker compose.base.yml up -d base-db

boreas-init:
	docker compose -f docker compose.boreas.yml up   -d boreas-db
	docker compose -f docker compose.boreas.yml exec -T boreas-db psql -U mvkt postgres -c '\l'
	docker compose -f docker compose.boreas.yml exec -T boreas-db dropdb -U mvkt --if-exists mvkt_db
	docker compose -f docker compose.boreas.yml exec -T boreas-db createdb -U mvkt -T template0 mvkt_db
	docker compose -f docker compose.boreas.yml exec -T boreas-db apt update
	docker compose -f docker compose.boreas.yml exec -T boreas-db apt install -y wget
	docker compose -f docker compose.boreas.yml exec -T boreas-db wget "https://snapshots.mvkt.io/tzkt_v1.14_parisnet.backup" -O mvkt_db.backup
	docker compose -f docker compose.boreas.yml exec -T boreas-db pg_restore -U mvkt -O -x -v -d mvkt_db -e -j 4 mvkt_db.backup
	docker compose -f docker compose.boreas.yml exec -T boreas-db rm mvkt_db.backup
	docker compose -f docker compose.boreas.yml exec -T boreas-db apt autoremove --purge -y wget
	docker compose pull	
	
boreas-start:
	docker compose -f docker compose.boreas.yml up -d

boreas-stop:
	docker compose -f docker compose.boreas.yml down

boreas-db-start:
	docker compose -f docker compose.boreas.yml up -d boreas-db
reset:
	docker compose -f docker compose.boreas.yml down --volumes
	docker compose -f docker compose.boreas.yml up -d boreas-db
