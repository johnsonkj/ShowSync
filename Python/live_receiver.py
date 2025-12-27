import zmq
import cv2
import numpy as np

context = zmq.Context()

# Receive frames
frame_socket = context.socket(zmq.PULL)
frame_socket.bind("tcp://*:5555")
frame_socket.RCVTIMEO = 100
frame_socket.RCVHWM = 1

# Send bool results
result_socket = context.socket(zmq.PUB)
result_socket.bind("tcp://*:5556")

cv2.namedWindow("Live Camera Feed", cv2.WINDOW_NORMAL)

def process_frame(frame) -> bool:
   
    return False

print("Python CV server running...")

try:
    while True:
        try:
            frame_bytes = frame_socket.recv()
        except zmq.Again:
            cv2.waitKey(1)
            continue

        npimg = np.frombuffer(frame_bytes, dtype=np.uint8)
        frame = cv2.imdecode(npimg, cv2.IMREAD_COLOR)
        if frame is None:
            continue

        result = process_frame(frame)

        # 🔹 Send result back (as string: "1" or "0")
        result_socket.send_string("1" if result else "0")

        cv2.imshow("Live Camera Feed", frame)
        if cv2.waitKey(1) & 0xFF == ord("q"):
            break

except KeyboardInterrupt:
    pass
finally:
    cv2.destroyAllWindows()
    frame_socket.close()
    result_socket.close()
    context.term()
