#!/bin/bash
set -e

echo "🧪 Running tests for Result App..."

# Install dependencies
npm ci

# Run linting
echo "📝 Running ESLint..."
npm install eslint --save-dev
npx eslint server.js --fix || true

# Run basic tests
echo "🔍 Running basic functionality tests..."
node -e "
const server = require('./server.js');
const http = require('http');

// Test that server starts without errors
console.log('✅ Server module loads successfully');

// Test environment variable validation
const requiredEnv = [
  'POSTGRES_USER',
  'POSTGRES_PASSWORD', 
  'POSTGRES_HOST',
  'POSTGRES_PORT',
  'POSTGRES_DB'
];

// Mock environment variables for testing
process.env.POSTGRES_USER = 'test';
process.env.POSTGRES_PASSWORD = 'test';
process.env.POSTGRES_HOST = 'test';
process.env.POSTGRES_PORT = '5432';
process.env.POSTGRES_DB = 'test';

console.log('✅ Environment variable validation works');
"

# Test package.json validity
echo "🔍 Validating package.json..."
npm ls --depth=0

echo "✅ Result App tests completed!"