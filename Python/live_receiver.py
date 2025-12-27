import zmq
import cv2
import numpy as np
import time
from mediapipe import solutions

# --- SOTA CONFIG ---
mp_hands = solutions.hands
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1, # Snapping usually tracked on one primary hand
    model_complexity=0, 
    min_detection_confidence=0.7,
    min_tracking_confidence=0.7
)

context = zmq.Context()
frame_socket = context.socket(zmq.PULL)
frame_socket.bind("tcp://*:5555")
frame_socket.setsockopt(zmq.RCVHWM, 1)

result_socket = context.socket(zmq.PUB)
result_socket.bind("tcp://*:5556")

# State Management for Snap
last_snap_dist = 1.0
last_snap_time = 0
is_pressed = False

def process_snap(frame, h, w) -> bool:
    global last_snap_dist, last_snap_time, is_pressed
    snap_triggered = False
    
    rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    results = hands.process(rgb_frame)
    
    if results.multi_hand_landmarks:
        for hand_landmarks in results.multi_hand_landmarks:
            # Landmark 4: Thumb Tip | Landmark 12: Middle Finger Tip
            thumb_tip = hand_landmarks.landmark[4]
            middle_tip = hand_landmarks.landmark[12]
            
            # Calculate 3D Distance
            dist = np.sqrt((thumb_tip.x - middle_tip.x)**2 + 
                           (thumb_tip.y - middle_tip.y)**2 + 
                           (thumb_tip.z - middle_tip.z)**2)
            
            # SNAP LOGIC:
            # 1. 'Press' phase: Distance becomes very small (< 0.05)
            # 2. 'Release' phase: Distance increases rapidly (The Snap)
            if dist < 0.05:
                is_pressed = True
            
            if is_pressed and dist > 0.08:
                curr = time.time()
                if curr - last_snap_time > 0.5: # 500ms Cooldown
                    snap_triggered = True
                    last_snap_time = curr
                    is_pressed = False # Reset for next snap

            # Visual Debugging
            color = (0, 255, 0) if is_pressed else (0, 0, 255)
            cv2.circle(frame, (int(thumb_tip.x*w), int(thumb_tip.y*h)), 8, color, -1)
            cv2.circle(frame, (int(middle_tip.x*w), int(middle_tip.y*h)), 8, color, -1)
            cv2.putText(frame, f"Gap: {dist:.3f}", (10, 60), 1, 1.2, (255, 255, 0), 2)

    return snap_triggered

print("SOTA Finger Snap Server Running... Port 5555/5556")

try:
    while True:
        frame_bytes = None
        while True:
            try: frame_bytes = frame_socket.recv(zmq.NOBLOCK)
            except zmq.Again: break
        
        if frame_bytes is None:
            cv2.waitKey(1)
            continue

        npimg = np.frombuffer(frame_bytes, dtype=np.uint8)
        frame = cv2.imdecode(npimg, cv2.IMREAD_COLOR)
        if frame is None: continue

        h, w, _ = frame.shape
        result = process_snap(frame, h, w)
        
        # Keep consistency with your Subscriber
        result_socket.send_string("1" if result else "0")

        if result:
            print("SNAP RECEIVED")
            cv2.putText(frame, "SNAP!", (w//3, h//2), 
                        cv2.FONT_HERSHEY_TRIPLEX, 2, (0, 255, 255), 5)

        cv2.imshow("Finger Snap Detection", frame)
        if cv2.waitKey(1) & 0xFF == ord("q"): break
finally:
    cv2.destroyAllWindows()
    frame_socket.close()
    result_socket.close()
    context.term()