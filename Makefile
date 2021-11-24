init:
	docker build -t tzkt-snapshot-dl -f Dockerfile-snapshot .
	docker run --name tzkt-snapshot tzkt-snapshot-dl
	docker cp tzkt-snapshot:/tzkt_db.backup .
	docker rm tzkt-snapshot
	docker-compose up -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db pg_restore -U tzkt -O -x -v -d tzkt_db -1 < tzkt_db.backup
	rm tzkt_db.backup
	docker rmi tzkt-snapshot-dl
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

db:
	docker-compose up -d db

migration:
	# Install EF: dotnet tool install --global dotnet-ef
	cd Tzkt.Data && dotnet-ef database update -s ../Tzkt.Sync/Tzkt.Sync.csproj

sync:
	export $$(cat .env | xargs) && dotnet run -p Tzkt.Sync -v normal

api:
	export $$(cat .env | xargs) && dotnet run -p Tzkt.Api -v normal

api-image:
	docker build -t bakingbad/tzkt-api:latest -f ./Tzkt.Api/Dockerfile .

sync-image:
	docker build -t bakingbad/tzkt-sync:latest -f ./Tzkt.Sync/Dockerfile .
	
granada-init:
	docker build -t tzkt-snapshot-granada -f Dockerfile-granada-snapshot .
	docker run --name tzkt-granada-snapshot tzkt-snapshot-granada
	docker cp tzkt-granada-snapshot:/granada_db.backup .
	docker rm tzkt-granada-snapshot
	docker-compose -f docker-compose.granada.yml up -d granada-db
	docker-compose -f docker-compose.granada.yml exec -T granada-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.granada.yml exec -T granada-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.granada.yml exec -T granada-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.granada.yml exec -T granada-db pg_restore -U tzkt -O -x -v -d tzkt_db -1 < granada_db.backup
	rm granada_db.backup
	docker rmi tzkt-snapshot-granada
	docker-compose pull	
	
granada-start:
	docker-compose -f docker-compose.granada.yml up -d

granada-stop:
	docker-compose -f docker-compose.granada.yml down

granada-db:
	docker-compose -f docker-compose.granada.yml up -d granada-db
	
hangzhou-init:
	docker-compose -f docker-compose.hangzhou.yml up -d   hangzhou-db
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db apt update
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db apt install -y wget
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db wget "https://tzkt.fra1.digitaloceanspaces.com/snapshots/tzkt_v1.6_hangzhou2net.backup" -O hangzhou_db.backup
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db pg_restore -U tzkt -O -x -v -d tzkt_db -e -j 8 hangzhou_db.backup
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db rm hangzhou_db.backup
	docker-compose -f docker-compose.hangzhou.yml exec -T hangzhou-db apt autoremove --purge -y wget
	docker-compose pull	
	
hangzhou-start:
	docker-compose -f docker-compose.hangzhou.yml up -d

hangzhou-stop:
	docker-compose -f docker-compose.hangzhou.yml down

hangzhou-db:
	docker-compose -f docker-compose.hangzhou.yml up -d hangzhou-db