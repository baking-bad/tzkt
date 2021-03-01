init:
#	docker run --name tzkt-snapshot bakingbad/tzkt-snapshot:latest
#	docker cp tzkt-snapshot:/tzkt_db.backup .
#	docker rm tzkt-snapshot
#	docker rmi bakingbad/tzkt-snapshot
	docker-compose up -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db pg_restore -U tzkt -O -x -v -d tzkt_db -1 < tzkt_db.backup
#	rm tzkt_db.backup
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
