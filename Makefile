init:
	docker run --name tzkt-snapshot bakingbad/tzkt-snapshot:latest
	docker cp tzkt-snapshot:/tzkt_db.backup .
	docker rm tzkt-snapshot
#	docker rmi bakingbad/tzkt-snapshot
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

pro-init:
	docker run --name tzkt-snapshot bakingbad/tzkt-snapshot:latest
	docker cp tzkt-snapshot:/tzkt_db.backup .
	docker rm tzkt-snapshot
#	docker rmi bakingbad/tzkt-snapshot
	docker-compose -f docker-compose.pro.yml up -d db
	docker-compose -f docker-compose.pro.yml exec -T db psql -U tzkt postgres -c '\l'
	docker-compose -f docker-compose.pro.yml exec -T db dropdb -U tzkt --if-exists tzkt_db
	docker-compose -f docker-compose.pro.yml exec -T db createdb -U tzkt -T template0 tzkt_db
	docker-compose -f docker-compose.pro.yml exec -T db pg_restore -U tzkt -O -x -v -d tzkt_db -1 < tzkt_db.backup
	docker-compose -f docker-compose.pro.yml exec -T db dropuser -U tzkt pro_user --if-exists
	docker-compose -f docker-compose.pro.yml exec -T db createuser -U tzkt pro_user -I -L
	docker-compose -f docker-compose.pro.yml exec -T db psql -U tzkt tzkt_db -c 'GRANT CONNECT ON DATABASE tzkt_db TO pro_user;'
	docker-compose -f docker-compose.pro.yml exec -T db psql -U tzkt tzkt_db -c 'GRANT USAGE ON SCHEMA public TO pro_user;'
	docker-compose -f docker-compose.pro.yml exec -T db psql -U tzkt tzkt_db -c 'GRANT SELECT ON ALL TABLES IN SCHEMA public TO pro_user;'
#	rm tzkt_db.backup
	docker-compose -f docker-compose.pro.yml build

pro-start:
	docker-compose -f docker-compose.pro.yml up -d

pro-stop:
	docker-compose -f docker-compose.pro.yml down

pro-update:
	git pull
	docker-compose -f docker-compose.pro.yml build