import sys
import argparse
import os
import os.path
import time
import uuid
import base64
from azure.storage import BlobService
from azure.storage import QueueService


QUEUE = 'handbackqueue'
CONTAINER = 'handback'

parser = argparse.ArgumentParser(description="Welcome to Omegatron 101 console client application!")
group = parser.add_mutually_exclusive_group()
parser.add_argument('-u','--username',help='Your username', required=True)
parser.add_argument('-t','--token',help='Your token', required=True)
group.add_argument('-up','--upload',help='Specify you want to upload files')
group.add_argument('-li','--list',action='store_true',help='List your files')
group.add_argument('-dl','--download',nargs='+',help='Download your files')
group.add_argument('-del','--delete',nargs='+',help='Delete files')
parser.add_argument('-d','--dir',help='Upload an entire directory recursively')
parser.add_argument('-f','--files',nargs='+', help='File(s) to be uploaded')
parser.add_argument('-de','--destination', help='Destination to download files, default is current folder')

args = parser.parse_args()

blob_service = BlobService(args.username, args.token)

queue_service = QueueService(args.username, args.token)

def upload_dir(directory):
    for (dirpath, dirnames, filenames) in os.walk(directory):
        for filename in filenames:
            upload_file(os.path.join(dirpath, filename))
    
def upload_file(file):
    UUID = uuid.uuid4()
    UUID = UUID.hex
    UUID_base = base64.b64encode(UUID)
    try:
        queue_service.put_message(QUEUE, UUID_base)
        blob_service.put_block_blob_from_path(CONTAINER, UUID, file)
    except Expection:
        print "Cant upload", file," , are you sure you have the permissions?"
        sys.exit(2)

def delete_file(file):
    blob_service.delete_blob(CONTAINER, file) 

def list_items():
    blobs = blob_service.list_blobs(CONTAINER)
    for blob in blobs:
        print(blob.name)
        print(blob.url)

def download_file(file, destination):
    if destination[-1] != '/':
        destination = destination + "/"
    try:
        blob_service.get_blob_to_path(CONTAINER, file, destination + file)
    except Exception:
        print "Cannot download", file ," , are you sure it exists?"
        sys.exit(2)

def delete_file(file):
    try:
        blob_service.delete_blob(CONTAINER, file) 
    except Exception:
        print "Cannot delete ",  file, " are you sure it exists?"
        sys.exit(2)

def file_permissions(file):
    file = os.path.abspath(file)

    if os.access(file, os.R_OK):
        return True
    return False
def file_exists(file):
    file = os.path.abspath(file)
    if os.path.isfile(file):
        return True
    return False

def check_subdir_permissions(dir):
    not_readable=[]
    for (dirpath, dirnames, filenames) in os.walk(dir):
        for dir in dirnames:
            if not os.access(os.path.join(dirpath, dir), os.R_OK):
                not_readable.append(os.path.join(dirpath, dir))
        for filename in filenames:
            if not os.access(os.path.join(dirpath, filename), os.R_OK  ):
                not_readable.append(os.path.join(dirpath, filename))
    return not_readable
              
def check_dir_exists(dir):
    if os.path.isdir(dir):
        return True

def main():
    blob_service = BlobService(args.username, args.token)
    queue_service = QueueService(args.username, args.token)

    if args.upload:
        if args.upload == 'dir' or args.upload == 'directory':
            if args.files:
                print "Please give a directory not a file!"
                sys.exit(2)
            if os.path.isfile(args.dir):
                print "Please give a directory not a file!"
                sys.exit(2)
            no_read = check_subdir_permissions(args.dir)
            no_file = check_dir_exists(args.dir)
            if no_read:
                print "The following dirs/files dont have read permissions, please fix this before attemping upload, program exits now"
                for file in no_read:
                    print file
                sys.exit(2)
            if not no_file:
                print "The following dir do not exist, program exits now"
                print args.dir
            upload_dir(args.dir)
        if args.upload == 'file' or args.upload == 'files':
            no_read = []
            no_file = []
            if args.dir:
                print "Please use only files with this option!"
                sys.exit(2)
            for exist in args.files:
                if os.path.isdir(exist):
                    print "The following is a directory and not a file! Please use only files with this option!"
                    print exist
                    sys.exit(2)
                if not file_permissions(exist):
                    no_read.append(exist)
                if not file_exists(exist):
                    no_file.append(exist)
            if no_read:
                print "The following files dont have read permissions, please fix this before attemping upload, program exits now"
                for file in no_read:
                    print file
                sys.exit(2)
            if no_file:
                print "The following files do not exist, program exits now"
                for file in no_file:
                    print file
                sys.exit(2)
            for file in args.files:
                upload_file(file)
        if args.upload != "file" and args.upload != 'files' and args.upload !='dir' and args.upload != 'directory':
            print "upload only takes the following arguments file,files,dir,directory, please see the help by invoking -h/--help"
            sys.exit(2)
    if args.delete:
        for file in args.delete:
            delete_file(file)

    if args.list:
        list_items()

    if args.download:
        if not args.destination:
            args.destination = '.'
        if not check_dir_exists(args.destination):
            print "Please make sure the directory exists before downloading"
            sys.exit(2)
        for file in args.download:
            download_file(file,args.destination)

if __name__ == "__main__":
    main()
