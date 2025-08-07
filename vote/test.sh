#!/bin/bash
set -e

echo "🧪 Running tests for Vote App..."

# Install dependencies
pip install -r requirements.txt

# Run Python linting
echo "📝 Running flake8 linting..."
pip install flake8
flake8 app.py --max-line-length=120 --ignore=E501,W503

# Run basic app tests
echo "🔍 Running basic functionality tests..."
python -c "
import app
import unittest

class TestVoteApp(unittest.TestCase):
    def setUp(self):
        app.app.config['TESTING'] = True
        self.client = app.app.test_client()
    
    def test_index_page(self):
        response = self.client.get('/')
        self.assertEqual(response.status_code, 200)
    
    def test_vote_endpoint_exists(self):
        # Test that vote endpoint accepts POST
        response = self.client.post('/', data={'vote': 'a'})
        # Should not return 404
        self.assertNotEqual(response.status_code, 404)

if __name__ == '__main__':
    unittest.main()
"

echo "✅ Vote App tests completed!"