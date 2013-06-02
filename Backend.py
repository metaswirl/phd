#!/usr/bin/python
from bottle import get, post, request, route, run, BaseRequest
import Image
import base64
import numpy
import logging
import logging.handlers
import sys
import urllib
import urllib2
from time import clock 
import mimetypes

BUCKET_ID = "9cb1f61b-49e1-4dae-8077-e7bdce9d7788"
SECRET_TOKEN = "ayVNE5bBnXPql9s8iWgt8x40ugacadsGSrPqwpKT"
URL = 'https://upload-api.kooaba.com/api/v4/buckets/' + BUCKET_ID + '/items'

def create_logger(): 
    """ initialize logging
    """
    logfile = "backend.log"
    formatter = '%(asctime)s [%(levelname)-8s] %(message)s'
    logging.basicConfig(
        filename = logfile,
        filemode = 'a',
        level = logging.DEBUG,
        format = formatter,
        datefmt = '%H:%M:%S'
    )
    log = logging.getLogger()

    to_stderr = logging.StreamHandler()
    to_stderr.setFormatter(logging.Formatter(formatter))
    log.addHandler(to_stderr)

    handler = logging.handlers.RotatingFileHandler(logfile, backupCount=9)
    handler.doRollover()

    return log

def encode_multipart_formdata(filename, title):
    """
    encode file as multipart/form-data
    """
    log.info("encoding picture to multipart/form-data")
    content_type, _encoding = mimetypes.guess_type(filename)
    with open(filename, 'rb') as f:
        content = f.read()

    BOUNDARY = '----------ThIs_Is_tHe_bouNdaRY_$'
    CRLF = '\r\n'
    L = []
    L.append('--' + BOUNDARY)
    L.append('Content-Disposition: form-data; name="title"')
    L.append('')
    L.append(title)
    L.append('--' + BOUNDARY)
    L.append('Content-Disposition: form-data; name="image"; filename="%s"' % filename)
    L.append('Content-Type: %s' % content_type )
    L.append('')
    L.append(content)
    L.append('--' + BOUNDARY + '--')
    L.append('')
    body = CRLF.join(L)
    content_type = 'multipart/form-data; boundary=%s' % BOUNDARY
    return content_type, bytearray(body)

def post_data(url, bucket_id, secret_token, content_type, data):
    """ post data to server, save reply
    """
    log.info("uploading picture to kooaba")
    data_key = 'Token ' + secret_token  
    #metadata = {'attribute1':'value1'}
    headers = {
        "Authorization" : data_key,
        "Content-Length" : str(len(data)),
        "Content-Type": content_type
    }
    #log.debug("headers %s" % headers)
    start = clock()
    try:
        req = urllib2.Request(url, data, headers)
        response = urllib2.urlopen(req)
        result = response.read()
    except Exception, e:
        print "Error"
        print e
    else:
        end = clock()
        return result, end - start

log = create_logger()
log.info("started app")
counter = 1
BaseRequest.MEMFILE_MAX = 10240000
log.debug("set MEMFILE_MAX to %d" % BaseRequest.MEMFILE_MAX)

@route('/actions/upload_picture', method='POST') 
def upload():
    log.info("received post request")
    global counter
    try:
        pic = request.forms['picture']
        width = request.forms['width']
        try:
          pic = base64.b64decode(pic)
        except TypeError as e:
          log.error("Base 64 encoding failed!")
          log.error(e)
          return "ERROR 1"
        size = (int(width), len(pic)/(3*int(width)))
        log.debug("length of picture %d size: %s" % (len(size), repr(size)))
        img = Image.frombuffer('RGB', size, pic, 'raw', 'RGB', 0, 1)

        title = 'photo-' + str(counter)
        filename = title + ".jpg"
        counter += 1

        log.debug("storing picture")
        img.save(title + ".jpg", 'JPEG')

        content_type, data = encode_multipart_formdata(filename, title)
        result, dur = post_data(URL, BUCKET_ID, SECRET_TOKEN, content_type, data)
        log.info("Uploading the picture took %d seconds" % dur)
        log.debug(str(result))
        return "OK green"

    except Exception as e:
        log.error("post failed")
        log.error(e)
        return "ERROR 2"

@get('/test')
def hello():
    return "Hello World!"

run(host='172.16.1.201', port=8080, debug=True)
