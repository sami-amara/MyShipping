mocha = ./node_modules/.bin/mocha
jshint = ./node_modules/.bin/jshint

test: lint
	@$(mocha) -t 6000

lint: ./lib/*.js
	@$(jshint) $^

clean:
	@rm -rf node_modules

.PHONY: test lint
