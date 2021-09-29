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
	
florence-init:
	docker build -t tzkt-snapshot-florence -f Dockerfile-florence-snapshot .
	docker run --name tzkt-florence-snapshot tzkt-snapshot-florence
	docker cp tzkt-florence-snapshot:/florence_db.backup .
	docker rm tzkt-florence-snapshot
	docker-compose -f docker-compose.florence.yml up -d florence-db
	docker-compose -f docker-compose.florence.yml exec -T florence-db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.florence.yml exec -T florence-db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.florence.yml exec -T florence-db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.florence.yml exec -T florence-db pg_restore -U tzkt -O -x -v -d tzkt_db -1 < florence_db.backup
	rm florence_db.backup
	docker rmi tzkt-snapshot-florence
	docker-compose pull
	
florence-start:
	docker-compose -f docker-compose.florence.yml up -d

florence-stop:
	docker-compose -f docker-compose.florence.yml down

florence-db:
	docker-compose -f docker-compose.florence.yml up -d florence-db