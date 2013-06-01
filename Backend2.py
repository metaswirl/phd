#!/usr/bin/python
from bottle import get, post, request, route, run, BaseRequest
import Image
import base64
import numpy

BaseRequest.MEMFILE_MAX = 10240000

@route('/actions/upload_picture', method='POST') # or @route('/login', method='POST')
def upload():
  print "foo"
  pic = base64.b64decode(request.forms['picture'])
  width = request.forms['width']
  print pic[-4:]
  size = (int(width), len(pic)/(3*int(width)))
  print width, len(pic) 
  print size
  img = Image.frombuffer('RGB', size, pic)
  img.show()

@get('/hello')
def hello():
    return "Hello World!"

run(host='172.16.1.201', port=8080, debug=True)
