init:
	docker build -t tzkt-snapshot-dl -f Dockerfile-snapshot .
	docker run --name tzkt-snapshot tzkt-snapshot-dl
	docker cp tzkt-snapshot:/tzkt_db.backup .
	docker rm tzkt-snapshot
	docker rmi tzkt-snapshot-dl
	docker-compose up -d db
	docker-compose exec -T db psql -U tzkt postgres -c '\l'
	docker-compose exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose exec -T db pg_restore -U tzkt -O -x -v -d tzkt_db -1 < tzkt_db.backup
	rm tzkt_db.backup
	docker-compose build

start:
	docker-compose up -d

stop:
	docker-compose down

update:
	git pull
	docker-compose build

clean:
	docker system prune --force
